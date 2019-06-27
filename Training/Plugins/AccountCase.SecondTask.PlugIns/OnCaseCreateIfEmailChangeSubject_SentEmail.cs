using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

using System;

namespace AccountCase.SecondTask.PlugIns
{
    public class OnCaseCreateIfEmailChangeSubject_SentEmail : IPlugin
    {
        //Plugin Constants
        private const string TARGET_ENTITY = "Target";
        private const string SUBJECT_ATTRIBUTE = "subjectid";
        private const string PREVIOUS_EMAIL_ATTRIBUTE = "new_previousemail";
        private const string TITLE = "title";
        private const string ACTIVITY_PARTY = "activityparty";
        private const string PARTY_ID = "partyid";
        private const string CONTACT = "contact";
        private const string PRIMARY_CONTACT_ID = "primarycontactid";
        private const string EMAIL_ADDRES_1 = "emailaddress1";
        private const string ACCOUNT = "account";
        private const string ADDRESS_USED = "addressused";

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
                tracingService.Trace("Depth of plug in OnCaseCreateIfEmailChangeSubject_SentEmail more than 2 return");
                return;
            }

            try
            {
                // Delete All traces for thise plugin
                DeleteAllTracesForThisPlugIn(service);

                tracingService.Trace("SET AS ASYNC: Start of plug in OnCaseCreateIfEmailChangeSubject_SentEmail");
                Entity target = null;

                if (context.InputParameters.Contains(TARGET_ENTITY)
                    && context.InputParameters[TARGET_ENTITY] is Entity)
                {
                    target = context.InputParameters[TARGET_ENTITY] as Entity;
                }
                var previousMail = target[PREVIOUS_EMAIL_ATTRIBUTE];
                tracingService.Trace(previousMail.ToString());

                Entity subject = null;
                if (target.Attributes.Contains(SUBJECT_ATTRIBUTE))
                {
                    EntityReference subjectId = (EntityReference)target.Attributes[SUBJECT_ATTRIBUTE];
                    subject = service.Retrieve("subject", subjectId.Id, new ColumnSet(TITLE));
                }
                
                //NB
                // CHECK teh CUID for SUBJECT not the TITLE
                if (subject == null
                    || !subject.Attributes.Contains(TITLE)
                    || subject.Attributes[TITLE].ToString() != "Email Change Request")
                {
                    tracingService.Trace("This case is not for changing emails");
                    return;
                }

                //Retreive system user credentials for FROM party
                //Plugin Context WhoAmI will be the person that is making the change. SYSTEM have to be the FROM party or SINGLE user

                WhoAmIRequest systemUserRequest = new WhoAmIRequest();
                WhoAmIResponse systemUserResponse = (WhoAmIResponse)service.Execute(systemUserRequest);
                Guid systemUserId = systemUserResponse.UserId;
                Entity fromActivityParty = new Entity(ACTIVITY_PARTY);
                fromActivityParty[PARTY_ID] = new EntityReference("systemuser", systemUserId);
                fromActivityParty[ADDRESS_USED] = "admin@plugin.com";
                tracingService.Trace($"FROM active party created Id: {systemUserId}");


                //Retreive Sending user credentials for To party
                EntityReference customerRef = (EntityReference)target.Attributes["customerid"];
                if (customerRef == null)
                {
                    tracingService.Trace($"Customer reference was not retrieved from customerid attribute");
                    return;
                }
                Entity caseCustomer = service.Retrieve(ACCOUNT, customerRef.Id, new ColumnSet(EMAIL_ADDRES_1));
                if (caseCustomer == null) // IF NULL CONTACT
                {
                    tracingService.Trace($"Customer was not retrieved from customerid: {customerRef.Id}");
                    return;
                }
                //NB
                //Customer field may be CONTACT have to check weather it is account or contact !!!!

                #region Not needed code to retrieve email from contact
                //EntityReference contactRef = (EntityReference)caseCustomer.Attributes[PRIMARY_CONTACT_ID];
                //if (contactRef ==null)
                //{
                //    tracingService.Trace("Contact reference was not retrieved from primarycontactid Attribute");
                //    return;
                //}
                //was true
                //Entity caseContact = service.Retrieve(CONTACT, contactRef.Id, new ColumnSet(true));
                //if (caseContact==null)
                //{
                //    tracingService.Trace($"Contact was not retrieved Id: {contactRef.Id}");
                //    return;
                //}

                #endregion

                Entity toActivityParty = new Entity(ACTIVITY_PARTY);
                toActivityParty[PARTY_ID] = new EntityReference(ACCOUNT, caseCustomer.Id);
                toActivityParty[ADDRESS_USED] = caseCustomer[EMAIL_ADDRES_1];

                Entity email = new Entity("email");
                email["from"] = new Entity[] { fromActivityParty };
                email["to"] = new Entity[] { toActivityParty };
                email["subject"] = "Email change request";
                email["description"] = "Dear Customer.Please CLICK to confirm you want to change the mail";
                email["directioncode"] = true;
                //NB
                //ADD REGARDING FIELD 

                Guid emailId = service.Create(email);
                tracingService.Trace($"Email with Id: {emailId} was created");
                SendEmailRequest sendEmailRequest = new SendEmailRequest
                {
                    EmailId = emailId,
                    TrackingToken = "",
                    IssueSend = true
                };

                SendEmailResponse sendEmailresp = (SendEmailResponse)service.Execute(sendEmailRequest);
                tracingService.Trace($"Email was sent!");

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
