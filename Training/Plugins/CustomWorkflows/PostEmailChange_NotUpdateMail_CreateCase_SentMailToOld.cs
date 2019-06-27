using System;

using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

using static CustomWorkflows.WorkflowConstants;
using Microsoft.Crm.Sdk.Messages;

namespace CustomWorkflows
{

    public class PostEmailChange_NotUpdateMail_CreateCase_SentMailToOld : CodeActivity
    {
        private const string WORKFLOW_NAME = "CustomWorkflows.PostEmailChange_NotUpdateMail_CreateCase_SentMailToOld";

        [Input("Account")]
        [RequiredArgument]
        [ReferenceTarget(ACCOUNT)]
        public InArgument<EntityReference> AccountReference { get; set; }
        

        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            // Use the context service to create an instance of IOrganizationService.             
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.InitiatingUserId);
            //Create the tracing service
            ITracingService tracingService = context.GetExtension<ITracingService>();
            tracingService.Trace($"Start of: {WORKFLOW_NAME}");

            if (workflowContext.Depth > 2)
            {
                tracingService.Trace("Depth is bigger than 1");
                return;
            }
            try
            {
                // Delete All traces for thise plugin
                DeleteAllTracesForThisPlugIn(service);

                //Retrieve Preimage for old mail
                Entity preImage = null;
                if (workflowContext.PreEntityImages.ContainsKey(PRE_BUISNESS_ENTITY))
                {
                    preImage = workflowContext.PreEntityImages[PRE_BUISNESS_ENTITY];
                }
                if (preImage == null)
                {
                    tracingService.Trace($"PreEntityImage is null");
                    return;
                }
                string oldEmail = preImage.GetAttributeValue<string>(EMAIL_ATTRIBUTE);

                //Retrieve PostImage for new mail
                Entity postImage = null;
                if (workflowContext.PostEntityImages.ContainsKey(POST_BUISNESS_ENTITY))
                {
                    postImage = workflowContext.PostEntityImages[POST_BUISNESS_ENTITY];
                }
                string newEmail = postImage.GetAttributeValue<string>(EMAIL_ATTRIBUTE);

                //Check for empty emails
                if (string.IsNullOrEmpty(oldEmail) || string.IsNullOrEmpty(newEmail))
                {
                    tracingService.Trace("Old or new email was found empty.");
                    return;
                }

                #region Anoter approach to access from OutArgument account and update Email
                //Save old mail NOT WORKING WHEN CREATIN case do not save teh OLD email
                //this.EmailOld.Set(context, oldEmail);
                #endregion

                //Retrieve account ref from InArgument
                EntityReference accountRef = this.AccountReference.Get(context);
                if (accountRef == null)
                {
                    tracingService.Trace("Account reference is null");
                    return;
                }
                Entity account = service.Retrieve(ACCOUNT, accountRef.Id, new ColumnSet(EMAIL_ATTRIBUTE));
                if (account == null)
                {
                    tracingService.Trace("Account is null");
                    return;
                }
                //Save old email
                account.Attributes[EMAIL_ATTRIBUTE] = oldEmail;
                service.Update(account);
                tracingService.Trace($"Account was updatet with old password - {oldEmail}");

                //Create Case 
                Entity newCaseEntity = CreateCase(service, accountRef.Id, oldEmail, newEmail);
                tracingService.Trace($"Case to create");
                //Save Case
                service.Create(newCaseEntity);
                tracingService.Trace("Case sucessfully created");

                //SENT EMAIL
                // Creat TO and FROM party
                Entity fromActivityParty = CreatFromPartySystem(service, tracingService);
                Entity toActivityParty = CreatToPartyChangedAccount(accountRef, account);

                //CreateEmail
                Entity email = CreatEmail(fromActivityParty, toActivityParty);

                Guid emailId = service.Create(email);
                tracingService.Trace($"Email with Id: {emailId} was created");
                SendEmailRequest sendEmailRequest = new SendEmailRequest
                {
                    EmailId = emailId,
                    TrackingToken = "",
                    IssueSend = true
                };

                service.Execute(sendEmailRequest);
                tracingService.Trace($"Email was sent!");
            }
            catch (Exception ex)
            {
                tracingService.Trace($@"Exception thrown Message: {ex.Message} 
                                        {Environment.NewLine} StackTrace: {ex.StackTrace}
                                        {Environment.NewLine} Inner Exception: {ex.InnerException}");
                throw new Exception(ex.Message);
            }            
        }

        private static Entity CreatEmail(Entity fromActivityParty, Entity toActivityParty)
        {

            //Creat email
            Entity email = new Entity("email");
            email["from"] = new Entity[] { fromActivityParty };
            email["to"] = new Entity[] { toActivityParty };
            email["subject"] = "Email change request";
            email["description"] = "Dear Customer.Please CLICK to confirm you want to change the mail";
            email["directioncode"] = true;

            return email;
        }

        private static Entity CreatToPartyChangedAccount(EntityReference accountRef, Entity account)
        {
            Entity toActivityParty = new Entity(ACTIVITY_PARTY);
            toActivityParty[PARTY_ID] = new EntityReference(ACCOUNT, accountRef.Id);
            toActivityParty[ADDRESS_USED] = account[EMAIL_ATTRIBUTE];
            return toActivityParty;
        }

        private static Entity CreatFromPartySystem(IOrganizationService service, ITracingService tracingService)
        {
            //TODO check how to sent from working user
            Entity fromActivityParty = new Entity(ACTIVITY_PARTY);
            WhoAmIRequest systemUserRequest = new WhoAmIRequest();
            WhoAmIResponse systemUserResponse = (WhoAmIResponse)service.Execute(systemUserRequest);

            Guid systemUserId = systemUserResponse.UserId;
            fromActivityParty[PARTY_ID] = new EntityReference("systemuser", systemUserId);
            fromActivityParty[ADDRESS_USED] = "admin@plugin.com";
            tracingService.Trace($"FROM active party created Id: {systemUserId}");

            return fromActivityParty;
        }

        private Entity CreateCase(IOrganizationService service, Guid targetId, string preImageEmail, string postImageEmail)
        {
            Entity incident = new Entity();

            incident.LogicalName = "incident";
            incident["title"] = "Email change";
            incident["description"] = "This is Email change request.";
            incident["customerid"] = new EntityReference(ACCOUNT, targetId);
            incident["new_changeemailstatus"] = new OptionSetValue(100000001); //InProgress ALL EXCEPTIONS AFTER THIS OptionsSet Included
            incident["new_previousemail"] = preImageEmail;
            incident[NEW_TO_CHANGE_EMAIL] = postImageEmail;
            incident["subjectid"] = new EntityReference("subject", Guid.Parse(SUBJECT_EMAIL_CHANGE_GUID));

            //incident.Attributes.Add("customerid", new EntityReference("account", targetId));
            return incident;
        }

        private void DeleteAllTracesForThisPlugIn(IOrganizationService service)
        {
            QueryExpression query = CreateQueryForTracesByPlugin(WORKFLOW_NAME);

            var tracesToDelete = service.RetrieveMultiple(query);
            foreach (var trace in tracesToDelete.Entities)
            {
                service.Delete(TRACE_LOG_ENTITY, trace.Id);
            }
        }

        private QueryExpression CreateQueryForTracesByPlugin(string workFlowName)
        {
            QueryExpression traceByPlugin = new QueryExpression(TRACE_LOG_ENTITY)
            {
                ColumnSet = new ColumnSet(TYPE_NAME)
            };
            traceByPlugin.Criteria.AddCondition(new ConditionExpression(TYPE_NAME, ConditionOperator.Equal, $"{workFlowName}"));

            return traceByPlugin;
        }

    }
}
