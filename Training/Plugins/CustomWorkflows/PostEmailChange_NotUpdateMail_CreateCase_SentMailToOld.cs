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
            #region Service instances and initail checkups

            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            // Use the context service to create an instance of IOrganizationService.             
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.InitiatingUserId);
            //Create the tracing service
            ITracingService tracingService = context.GetExtension<ITracingService>();
            tracingService.Trace($"Start of: {WORKFLOW_NAME}");

            if (workflowContext.Depth >= 2)
            {
                tracingService.Trace($"Depth is bigger than 1. Actual Depth: {workflowContext.Depth}");
                return;
            }
            if (workflowContext.MessageName.ToLower() != "update")
            {
                tracingService.Trace($"Context message was different than Update. It was: {workflowContext.MessageName}");
                return;
            }
            #endregion
            try
            {
                #region Delete previous traces

                // Delete All traces for thise plugin
                DeleteAllTracesForThisPlugIn(service);
                #endregion

                #region Pre and Post Image retrieval

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
                #endregion
               

                //Retrieve account ref from InArgument
                EntityReference accountRef = this.AccountReference.Get(context);
                if (accountRef == null)
                {
                    tracingService.Trace("Account reference is null");
                    return;
                }
                
                //Save old email
                var accountToUpdate = new Entity(ACCOUNT);
                accountToUpdate.Id = accountRef.Id;
                accountToUpdate.Attributes[EMAIL_ATTRIBUTE] = oldEmail;
                service.Update(accountToUpdate);
                tracingService.Trace($"Account was updatet with old password - {oldEmail}");


                //Create Case 
                Entity newCaseEntity = CreateCase(service, accountToUpdate.Id, oldEmail, newEmail);
                tracingService.Trace($"Case to create");
                //Save Case
                var caseId = service.Create(newCaseEntity);
                tracingService.Trace("Case sucessfully created");

                //SENT EMAIL
                // Creat TO and FROM party
                Entity fromActivityParty = CreatFromPartySystem(workflowContext, tracingService);
                Entity toActivityParty = CreatToPartyChangedAccount(accountToUpdate);

                //CreateEmail
                Entity email = CreatEmail(fromActivityParty, toActivityParty, caseId);

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

        private static Entity CreatEmail(Entity fromActivityParty, Entity toActivityParty, Guid caseId)
        {

            //Creat email
            Entity email = new Entity("email");
            email["from"] = new Entity[] { fromActivityParty };
            email["to"] = new Entity[] { toActivityParty };
            email["subject"] = "Email change request";
            email["description"] = "WORKFLOW. Dear Customer.Please CLICK to confirm you want to change the mail";
            email["directioncode"] = true;
            email["regardingobjectid"] = new EntityReference("incident", caseId);

            return email;
        }

        private static Entity CreatToPartyChangedAccount(Entity account)
        {
            Entity toActivityParty = new Entity(ACTIVITY_PARTY);
            toActivityParty[PARTY_ID] = new EntityReference(ACCOUNT, account.Id);
            toActivityParty[ADDRESS_USED] = account[EMAIL_ATTRIBUTE];

            return toActivityParty;
        }

        private static Entity CreatFromPartySystem(IWorkflowContext workfloWContext, ITracingService tracingService)
        {            
            Entity fromActivityParty = new Entity(ACTIVITY_PARTY);

            //WhoAmIRequest systemUserRequest = new WhoAmIRequest();
            //WhoAmIResponse systemUserResponse = (WhoAmIResponse)service.Execute(systemUserRequest);
            //Guid systemUserId = systemUserResponse.UserId;

            fromActivityParty[PARTY_ID] = new EntityReference("systemuser", workfloWContext.UserId);
            fromActivityParty[ADDRESS_USED] = "admin@plugin.com";
            tracingService.Trace($"FROM active party created Id: {workfloWContext.UserId}");

            return fromActivityParty;
        }

        private Entity CreateCase(IOrganizationService service, Guid targetId, string oldEmail, string newEmail)
        {
            Entity incident = new Entity();

            incident.LogicalName = "incident";
            incident["title"] = "Email change";
            incident["description"] = "This is Email change request.";
            incident["customerid"] = new EntityReference(ACCOUNT, targetId);
            incident["new_changeemailstatus"] = new OptionSetValue(100000001); //InProgress
            incident["new_previousemail"] = oldEmail;
            incident[NEW_TO_CHANGE_EMAIL] = newEmail;
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
