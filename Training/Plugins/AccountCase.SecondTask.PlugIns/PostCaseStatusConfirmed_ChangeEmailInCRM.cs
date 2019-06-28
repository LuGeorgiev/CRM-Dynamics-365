using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

using System;
using System.ServiceModel;
using System.Collections.Generic;
using System.Linq;

namespace AccountCase.SecondTask.PlugIns
{
    public class PostCaseStatusConfirmed_ChangeEmailInCRM : IPlugin
    {
        //Image Constants
        private readonly string preImageAlias = "PreImage";
        private readonly string postImageAlias = "PostImage";

        //Plugin constants
        private const string ACCOUNT = "account";
        private const string CURRENT_PLUGIN_NAME = "PostCaseStatusConfirmed_ChangeEmailInCRM";
        private const string TARGET_ENTITY = "Target";
        private const string CUSTOMER_ID = "customerid";
        private const string CREATED_ON = "createdon";
        private const string CONTACT = "contact";
        private const string NEW_CHANGE_EMAIL_STATUS = "new_changeemailstatus";
        private const string NEW_CHANGE_EMAIL_STATUS_REASON = "new_changeemailstatusreason";
        private const string NEW_TO_CHANGE_EMAIL = "new_tochangeemail";
        private const string STATE_CODE = "statecode";
        private const string STATUS_CODE = "statuscode";
        private const string INCIDENT_ID = "incidentid";
        private const string EMAIL_ATTRIBUTE = "emailaddress1";
        private const string INCIDENT = "incident";
        private const string INCIDENT_RESOLUTION = "incidentresolution";
        private const string SUBJECT = "subject";
        private const string SUBJECT_EMAIL_CHANGE_GUID = "D5911B1A-DA8D-E911-A81B-000D3ABA3097";

        //Delete Plugin trace constants
        private const string PLUGIN_TRACELOG_NAME = "PluginProfiler.Plugins.ProfilerPlugin";
        private const string TRACE_LOG_ENTITY = "plugintracelog";
        private const string TYPE_NAME = "typename";

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth >= 2)
            {
                tracingService.Trace($"Depth of plug in {CURRENT_PLUGIN_NAME} more than 1 return. Current depth: {context.Depth}");
                return;
            }
            if (context.MessageName.ToLower() != "update")
            {
                tracingService.Trace($"Context message was different than Update. It was: {context.MessageName}");
                return;
            }

            try
            {
                // Delete All traces for thise plugin
                DeleteAllTracesForThisPlugIn(service);
                tracingService.Trace($"Start of plug in {CURRENT_PLUGIN_NAME}");

                Entity preImageEntity = (context.PreEntityImages != null
                                        && context.PreEntityImages.Contains(this.preImageAlias))
                                                ? context.PreEntityImages[this.preImageAlias]
                                                : null;                

                Entity postImageEntity = (context.PostEntityImages != null
                                         && context.PostEntityImages.Contains(this.postImageAlias))
                                                ? context.PostEntityImages[this.postImageAlias]
                                                : null;

                var preImageEmailChangeStatus = preImageEntity?.GetAttributeValue<OptionSetValue>(NEW_CHANGE_EMAIL_STATUS)?.Value;
                var postImageEmailChangeStatus = postImageEntity?.GetAttributeValue<OptionSetValue>(NEW_CHANGE_EMAIL_STATUS)?.Value;

                //CHECK if status was changed from InProcess to Confirm or RETURN          

                if (preImageEmailChangeStatus == null
                    || postImageEmailChangeStatus == null
                    || preImageEmailChangeStatus != 100000001 //InProcess
                    || postImageEmailChangeStatus != 100000002) //Confirmed
                {
                    tracingService.Trace("Transiotion from InProcess to Confirmed not observed or null - return!");
                    return;
                }


                Entity target = null;
                if (context.InputParameters.Contains(TARGET_ENTITY)
                    && context.InputParameters[TARGET_ENTITY] is Entity)
                {
                    target = context.InputParameters[TARGET_ENTITY] as Entity;
                }
                var incidentId = target.Attributes[INCIDENT_ID].ToString();

                // ACCOUNT OR CONTACT CHECK
                var incident = service.Retrieve(INCIDENT, Guid.Parse(incidentId), new ColumnSet(CUSTOMER_ID));
                var customerRef = (EntityReference)incident.Attributes[CUSTOMER_ID];
                Entity customer = service.Retrieve(ACCOUNT, customerRef.Id, new ColumnSet(EMAIL_ATTRIBUTE));
                bool isContactInCustomer = false;

                // IF NULL customer try if customerid is CONTACT not ACCOUNT
                if (customer == null)
                {
                    customer = service.Retrieve(CONTACT, customerRef.Id, new ColumnSet(EMAIL_ATTRIBUTE));

                    if (customer == null)
                    {
                        tracingService.Trace($"Customer was not retrieved from customerid: {customerRef.Id}");
                        return;
                    }

                    isContactInCustomer = true;
                }

                QueryExpression query = CreateQuery(customerRef);
                var entityCollection = service.RetrieveMultiple(query);

                var cases = new List<Entity>();
                cases.AddRange(entityCollection.Entities.ToList());

                tracingService.Trace($"Cases retreived: {cases.Count}");

                //Create new object with the same GUID and update only fields that are needed !!! NEW OBJECT ALWAYS
                var caseToSolve = new Entity(INCIDENT);
                caseToSolve.Id = cases[0].Id;

                caseToSolve[NEW_CHANGE_EMAIL_STATUS] = new OptionSetValue(100000004); //Approved status of ChangeEmailStatus field
                caseToSolve[NEW_CHANGE_EMAIL_STATUS_REASON] = new OptionSetValue(100000002);//Approved - valid request
                service.Update(caseToSolve);

                string newMail = cases[0][NEW_TO_CHANGE_EMAIL].ToString();

                //Resolve last case
                Entity caseResolution = new Entity(INCIDENT_RESOLUTION);
                caseResolution.Attributes.Add(INCIDENT_ID, new EntityReference(INCIDENT, caseToSolve.Id));
                caseResolution.Attributes.Add(SUBJECT, "Parent Case has been resolved");

                CloseIncidentRequest closeReq = new CloseIncidentRequest
                {
                    IncidentResolution = caseResolution,
                    RequestName = "CloseIncident",
                    Status = new OptionSetValue(5)
                };

                service.Execute(closeReq);
                tracingService.Trace($"Case Id: {caseToSolve.Id} resolved");

                for (int i = 1; i < cases.Count; i++)
                {
                    //Cancel all other cases
                    var currentCase = new Entity(INCIDENT);
                    currentCase.Id = cases[i].Id;
                    currentCase[NEW_CHANGE_EMAIL_STATUS] = new OptionSetValue(100000003); //Declined status of ChangeEmailStatus field
                    currentCase[NEW_CHANGE_EMAIL_STATUS_REASON] = new OptionSetValue(100000001);//Cancelled-Duplicated record
                    service.Update(currentCase);
                                        
                    SetStateRequest request = new SetStateRequest()
                    {
                        EntityMoniker = new EntityReference(INCIDENT, currentCase.Id),
                        State = new OptionSetValue(2),
                        Status = new OptionSetValue(6)
                    };
                    service.Execute(request);
                    tracingService.Trace($"Case Id: {currentCase.Id} closed!");

                    #region not Working Approach for closing casese
                    //caseResolution = new Entity(INCIDENT_RESOLUTION);
                    //caseResolution.Attributes.Add(INCIDENT_ID, new EntityReference(INCIDENT,currentCase.Id));
                    //caseResolution.Attributes.Add(SUBJECT, "Parent Case has been cancelled");
                    //closeReq = new CloseIncidentRequest
                    //{
                    //    IncidentResolution = caseResolution,
                    //    RequestName = "CancellIncident",
                    //    //Cancelled VALUE
                    //    Status = new OptionSetValue(6)
                    //};                   

                    //This approach THROWS:
                    //$exception  { "CancellIncident#2011/Organization.svc"}
                    //System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>
                    //service.Execute(closeReq);
                    #endregion
                }

                //var accountToChangeEmail = service.Retrieve("account", customer.Id, new ColumnSet(EMAIL_ATTRIBUTE));

                //NEW OBJECT WITH this GUID
                if (isContactInCustomer)
                {
                    var contact = new Entity(CONTACT);
                    contact.Id = customer.Id;
                    contact.Attributes[EMAIL_ATTRIBUTE] = newMail;

                    service.Update(contact);
                    tracingService.Trace($"Email of contact {customer.Id} was changed to {newMail}");
                }
                else
                {
                    var account = new Entity(ACCOUNT);
                    account.Id = customer.Id;
                    account.Attributes[EMAIL_ATTRIBUTE] = newMail;

                    service.Update(account);
                    tracingService.Trace($"Email of account {customer.Id} was changed to {newMail}");
                }
            }
            catch (InvalidPluginExecutionException ex)
            {

                tracingService.Trace($"Exception: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private static QueryExpression CreateQuery(EntityReference customer)
        {
            var query = new QueryExpression(INCIDENT)
            {
                ColumnSet = new ColumnSet(CREATED_ON, 
                                          STATE_CODE, 
                                          INCIDENT_ID, 
                                          NEW_CHANGE_EMAIL_STATUS, 
                                          NEW_CHANGE_EMAIL_STATUS_REASON, 
                                          NEW_TO_CHANGE_EMAIL)
            };

            query.Criteria.AddCondition(new ConditionExpression(STATE_CODE, ConditionOperator.Equal, 0));
            query.Criteria.AddCondition(new ConditionExpression(CUSTOMER_ID, ConditionOperator.Equal, customer.Id));
            query.Criteria.AddCondition(new ConditionExpression("subjectid", ConditionOperator.Equal, SUBJECT_EMAIL_CHANGE_GUID ));
            //query.Criteria.AddCondition(new ConditionExpression("title", ConditionOperator.Equal, "Email change")); // BETTER SUBJECT GUID NOT STRING

            query.Criteria.FilterOperator = LogicalOperator.And;
            query.Orders.Add(new OrderExpression(CREATED_ON, OrderType.Descending));

            return query;
        }

        private void DeleteAllTracesForThisPlugIn(IOrganizationService service)
        {
            QueryExpression query = CreateQueryForTracesByPlugin(PLUGIN_TRACELOG_NAME);

            var tracesToDelete = service.RetrieveMultiple(query);
            foreach (var trace in tracesToDelete.Entities)
            {
                service.Delete(TRACE_LOG_ENTITY, trace.Id);
            }
        }

        private QueryExpression CreateQueryForTracesByPlugin(string pluginName)
        {
            QueryExpression traceByPlugin = new QueryExpression(TRACE_LOG_ENTITY)
            {
                ColumnSet = new ColumnSet(TYPE_NAME)
            };
            traceByPlugin.Criteria.AddCondition(new ConditionExpression(TYPE_NAME, ConditionOperator.Equal, $"{pluginName}"));

            return traceByPlugin;
        }
    }
}
