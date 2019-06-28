using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountCase.SecondTask.PlugIns
{
    public class PostEmailChangeBySystem_SentEmail : IPlugin
    {
        //Plugin constants
        private const string TARGET_ENTITY = "Target";
        private const string EMAIL_ATTRIBUTE = "emailaddress1";
        private const string ACTIVITY_PARTY = "activityparty";
        private const string CONTACT = "contact";
        private const string PARTY_ID = "partyid";
        private const string ACCOUNT = "account";
        private const string ADDRESS_USED = "addressused";
        private const string CUSTOMER_ID = "customerid";
        private const string STATE_CODE = "statecode";
        private const string TITLE = "title";
        private const string NEW_CHANGE_EMAIL_STATUS = "new_changeemailstatus";
        private const string SUBJECT_ID = "subjectid";
        private const string SUBJECT_EMAIL_CHANGE_GUID = "D5911B1A-DA8D-E911-A81B-000D3ABA3097";

        //Delete Plugin trace constants
        private const string PLUGIN_TRACELOG_NAME = "AccountCase.SecondTask.PlugIns.PostEmailChangeBySystem_SentEmail";
        private const string TRACE_LOG_ENTITY = "plugintracelog";
        private const string TYPE_NAME = "typename";

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //Izzat 
            //I can perfectly implement the approach with context.Depth==3. I had that idea but to my understanding this is very unstable approach and may lead to random triggers in large system.
            //That is why I will stick to case Resolve. If you insist I will do it again. Coding is pleasure, after all :)

            //Check Depth and trace
            if (context.Depth > 3)
            {
                tracingService.Trace($"Depth of plugin {PLUGIN_TRACELOG_NAME} more than 2 will return. Depth is {context.Depth}");
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

                tracingService.Trace($"Start of {PLUGIN_TRACELOG_NAME}");

                //Retrieve target and entity
                Guid? targetId = null;
                Entity target = null;

                if (context.InputParameters.Contains(TARGET_ENTITY) && context.InputParameters[TARGET_ENTITY] is Entity)
                {
                    target = (Entity)context.InputParameters[TARGET_ENTITY];
                    tracingService.Trace($"Entity {target.LogicalName} with id: {target.Id} retrieved");

                    targetId = target.Id;
                }
                var incident = service.Retrieve("incident", targetId.Value, new ColumnSet(new[] { NEW_CHANGE_EMAIL_STATUS, CUSTOMER_ID, STATE_CODE, SUBJECT_ID }));
                var customerReference = incident?.GetAttributeValue<EntityReference>(CUSTOMER_ID);
                var changeEmailStatus = incident?.GetAttributeValue<OptionSetValue>(NEW_CHANGE_EMAIL_STATUS);
                var statusCode = incident?.GetAttributeValue<OptionSetValue>(STATE_CODE);
                var subjectId = incident?.GetAttributeValue<EntityReference>(SUBJECT_ID);

                bool anyIsNull = customerReference == null
                    || changeEmailStatus == null
                    || statusCode == null
                    || subjectId == null;
                if (anyIsNull)
                {
                    tracingService.Trace("Any target value was null");
                    return;
                }
                // IF Account is the trigger Depth ==2 !!!!


                bool isSentMailNeeded = changeEmailStatus.Value == 100000004 //Resolved
                                        && statusCode.Value == 1
                                        && subjectId.Id == Guid.Parse(SUBJECT_EMAIL_CHANGE_GUID);
                if (!isSentMailNeeded)
                {
                    tracingService.Trace("Input values are not for that case");
                    return;
                }

                //Create of TO party
                var caseCustomer = service.Retrieve(ACCOUNT, customerReference.Id, new ColumnSet(EMAIL_ATTRIBUTE));
                bool isContactInCustomer = false;

                // IF NULL customer try if customerid is CONTACT not ACCOUNT
                if (caseCustomer == null)
                {
                    caseCustomer = service.Retrieve(CONTACT, customerReference.Id, new ColumnSet(EMAIL_ATTRIBUTE));

                    if (caseCustomer == null)
                    {
                        tracingService.Trace($"Customer was not retrieved from customerid: {customerReference.Id}");
                        return;
                    }

                    isContactInCustomer = true;
                }
                tracingService.Trace($"account to sent mail retirved Id: {caseCustomer.Id}");

                var emailToSentTo = caseCustomer.GetAttributeValue<string>(EMAIL_ATTRIBUTE);
                if (emailToSentTo == null)
                {
                    tracingService.Trace($"Email to sent info is missing!");
                    return;
                }

                //Formin TO party
                Entity toActivityParty = new Entity(ACTIVITY_PARTY);
                if (isContactInCustomer)
                {
                    toActivityParty[PARTY_ID] = new EntityReference(CONTACT, caseCustomer.Id);
                }
                else
                {
                    toActivityParty[PARTY_ID] = new EntityReference(ACCOUNT, caseCustomer.Id);
                }
                toActivityParty[ADDRESS_USED] = caseCustomer[EMAIL_ATTRIBUTE];

                //Form FROM party                
                Entity fromActivityParty = new Entity(ACTIVITY_PARTY);
                fromActivityParty[PARTY_ID] = new EntityReference("systemuser", context.UserId);
                fromActivityParty[ADDRESS_USED] = "admin@plugin.com";
                tracingService.Trace($"FROM active party created Id: {context.UserId}");

                //Sent Email
                Entity email = new Entity("email");
                email["from"] = new Entity[] { fromActivityParty };
                email["to"] = new Entity[] { toActivityParty };
                email["subject"] = "Email change request";
                email["description"] = $"Dear Customer. Your email was changed. From now on you can access us only from {emailToSentTo}";
                email["directioncode"] = true;
                email["regardingobjectid"] = new EntityReference("incident", target.Id);

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
            catch (InvalidPluginExecutionException ex)
            {
                tracingService.Trace($"Exception: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message);
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
