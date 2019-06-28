using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

using System;

namespace AccountCase.SecondTask.PlugIns
{
    public class OnCaseCreateIfEmailChangeSubject_SentEmail : IPlugin
    {
        //Plugin Constants
        private const string ACCOUNT = "account";
        private const string ACTIVITY_PARTY = "activityparty";
        private const string ADDRESS_USED = "addressused";
        private const string CONTACT = "contact";
        private const string EMAIL_ADDRES_1 = "emailaddress1";
        private const string PARTY_ID = "partyid";
        private const string PREVIOUS_EMAIL_ATTRIBUTE = "new_previousemail";
        private const string PRIMARY_CONTACT_ID = "primarycontactid";
        private const string SUBJECT_ATTRIBUTE = "subjectid";
        private readonly Guid SUBJECT_EMAIL_CHANGE_GUID = Guid.Parse("D5911B1A-DA8D-E911-A81B-000D3ABA3097");
        private const string TARGET_ENTITY = "Target";
        private const string TITLE = "title";

        //Delete Plugin trace constants
        private const string PLUGIN_TRACELOG_NAME = "AccountCase.SecondTask.PlugIns.OnCaseCreateIfEmailChangeSubject_SentEmail";
        private const string TRACE_LOG_ENTITY = "plugintracelog";
        private const string TYPE_NAME = "typename";


        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth > 2)
            {
                tracingService.Trace($"Depth of plug in OnCaseCreateIfEmailChangeSubject_SentEmail more than 2 return. Depth value: {context.Depth}");
                return;
            }
            if (context.MessageName.ToLower() != "create")
            {
                tracingService.Trace($"Context message was different than Create. It was: {context.MessageName}");
                return;
            }

            try
            {
                // Delete All traces for thise plugin
                DeleteAllTracesForThisPlugIn(service);
                tracingService.Trace("Start of plug in OnCaseCreateIfEmailChangeSubject_SentEmail");

                //OnCase Create
                Entity targetCase = null;
                if (context.InputParameters.Contains(TARGET_ENTITY)
                    && context.InputParameters[TARGET_ENTITY] is Entity)
                {
                    targetCase = context.InputParameters[TARGET_ENTITY] as Entity;
                }
                var previousMail = targetCase[PREVIOUS_EMAIL_ATTRIBUTE];
                tracingService.Trace(previousMail.ToString());

                //Entity subject = null;
                EntityReference subjectId = null;
                if (targetCase.Attributes.Contains(SUBJECT_ATTRIBUTE))
                {
                     subjectId = (EntityReference)targetCase.Attributes[SUBJECT_ATTRIBUTE];
                }                
                
                // CHECK teh GUID for SUBJECT not the TITLE
                if (subjectId == null
                    || subjectId.Id != SUBJECT_EMAIL_CHANGE_GUID)
                {
                    tracingService.Trace("This case is not for changing emails");
                    return;
                }

                //Code to retire
                //WhoAmIRequest systemUserRequest = new WhoAmIRequest();
                //WhoAmIResponse systemUserResponse = (WhoAmIResponse)service.Execute(systemUserRequest);                
                //Guid systemUserId = systemUserResponse.UserId;
                //fromActivityParty[PARTY_ID] = new EntityReference("systemuser", systemUserId);

                //Retreive system user credentials for FROM party HOW TO SENT FROM SYSTEM
                Entity fromActivityParty = new Entity(ACTIVITY_PARTY);
                fromActivityParty[PARTY_ID] = new EntityReference("systemuser", context.UserId);
                fromActivityParty[ADDRESS_USED] = "admin@plugin.com";
                tracingService.Trace($"FROM active party created Id: {context.UserId}");

                //Retreive Sending user credentials for To party
                //TODO check if customerid is CONTACT not ACCOUNT
                EntityReference customerRef = (EntityReference)targetCase.Attributes["customerid"];
                if (customerRef == null)
                {
                    tracingService.Trace($"Customer reference was not retrieved from customerid attribute");
                    return;
                }

                Entity caseCustomer = service.Retrieve(ACCOUNT, customerRef.Id, new ColumnSet(EMAIL_ADDRES_1));
                bool isContactInCustomer = false;

                // IF NULL customer try if customerid is CONTACT not ACCOUNT
                if (caseCustomer == null) 
                {
                    caseCustomer = service.Retrieve(CONTACT, customerRef.Id, new ColumnSet(EMAIL_ADDRES_1));

                    if (caseCustomer == null)
                    {
                        tracingService.Trace($"Customer was not retrieved from customerid: {customerRef.Id}");
                        return;
                    }

                    isContactInCustomer = true;
                }
                                
                Entity toActivityParty = new Entity(ACTIVITY_PARTY);
                if (isContactInCustomer)
                {
                    toActivityParty[PARTY_ID] = new EntityReference(CONTACT, caseCustomer.Id);
                }
                else
                {
                    toActivityParty[PARTY_ID] = new EntityReference(ACCOUNT, caseCustomer.Id);
                }
                toActivityParty[ADDRESS_USED] = caseCustomer[EMAIL_ADDRES_1];

                Entity email = new Entity("email");
                email["from"] = new Entity[] { fromActivityParty };
                email["to"] = new Entity[] { toActivityParty };
                email["subject"] = "Email change request";
                email["description"] = "Dear Customer.Please CLICK to confirm you want to change the mail";
                email["directioncode"] = true;
                //NB!! regarding is very important
                email["regardingobjectid"] = new EntityReference("incident",targetCase.Id);
                
                Guid emailId = service.Create(email);
                tracingService.Trace($"Email with Id: {emailId} was created");
                SendEmailRequest sendEmailRequest = new SendEmailRequest
                {
                    EmailId = emailId,
                    TrackingToken = "",
                    IssueSend = true
                };

                SendEmailResponse sendEmailresp = (SendEmailResponse)service.Execute(sendEmailRequest);
                tracingService.Trace($"Email was sent! Result: {sendEmailresp.Results}");

            }
            catch (Exception ex)
            {
                tracingService.Trace(ex.Message);
                throw;
            }

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
