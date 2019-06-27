using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;

using static CustomWorkflows.WorkflowConstants;

namespace CustomWorkflows
{
    public class PostCaseConfirm_UpdateAllAccountCases_ChangeMail_SentMail : CodeActivity
    {
        private const string WORKFLOW_NAME = "CustomWorkflows.PostCaseConfirm_UpdateAllAccountCases_ChangeMail_SentMail";

        [Input("Triggered case")]
        [RequiredArgument]
        [ReferenceTarget(INCIDENT)]
        public InArgument<EntityReference> TargetCase { get; set; }

        [Input("Case Title")]
        [RequiredArgument]
        [AttributeTarget(INCIDENT,TITLE)]
        public InArgument<string> Title { get; set; }

        [Input("Case customer")]
        [RequiredArgument]
        [ReferenceTarget(ACCOUNT)]
        public InArgument<EntityReference> CaseCustomer { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            #region Services, Context and DepthCheck

            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            //Create the tracing service
            ITracingService tracingService = context.GetExtension<ITracingService>();

            tracingService.Trace($"Start of: {WORKFLOW_NAME}");
            if (workflowContext.Depth >= 2)
            {
                tracingService.Trace("Depth is bigger than 1");
                return;
            }
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            // Use the context service to create an instance of IOrganizationService.             
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.InitiatingUserId);

            #endregion

            try
            {
                #region Delete previous traces from that workflow

                // Delete All traces for this workflow
                DeleteAllTracesForThisPlugIn(service);
                tracingService.Trace("Old traces were deleted");
                #endregion

                #region Input Retrieval and validation 

                EntityReference incidentRef = this.TargetCase.Get(context);
                if (incidentRef == null)
                {
                    tracingService.Trace("TargetCase reference is null");
                    return;
                }

                var title = this.Title.Get(context);
                if (string.IsNullOrEmpty(title) || title != EMAIL_CHANGE)
                {
                    tracingService.Trace("This case is for Email change only!");
                    return;
                }

                EntityReference accountRef = this.CaseCustomer.Get(context);
                if (incidentRef == null)
                {
                    tracingService.Trace("TargetCase reference is null");
                    return;
                }
                //Case info
                var accountName = accountRef.Name;
                tracingService.Trace($"IncidentRef: {incidentRef.Id} {Environment.NewLine} CustomerReference: {accountRef.Id} with name {accountName} {Environment.NewLine} Case title: {title}");

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
                var oldEmailChangeStatus = preImage.GetAttributeValue<OptionSetValue>(NEW_CHANGE_EMAIL_STATUS)?.Value;
                //tracingService.Trace($"OldEmail change status from Pre Image: {oldEmailChangeStatus.Value}");

                Entity postImage = null;
                if (workflowContext.PostEntityImages.ContainsKey(POST_BUISNESS_ENTITY))
                {
                    postImage = workflowContext.PostEntityImages[POST_BUISNESS_ENTITY];
                }
                if (postImage == null)
                {
                    tracingService.Trace($"PreEntityImage is null");
                    return;
                }
                var newEmailChangeStatus = postImage.GetAttributeValue<OptionSetValue>(NEW_CHANGE_EMAIL_STATUS)?.Value;
                //tracingService.Trace($"NewEmail change status from Post Image: {newEmailChangeStatus.Value} ");

                if (oldEmailChangeStatus == null
                    || newEmailChangeStatus == null
                    || oldEmailChangeStatus != 100000001 // In Process
                    || newEmailChangeStatus != 100000002) // Confirmed
                {
                    tracingService.Trace("Condition: oldEmailStatus to be InProcess and newEmailStatus to be Confirmed was not met!");
                    return;
                }
                #endregion

                #region Resolve or Cancel all active cases with Emal change title that belongs to account

                QueryExpression query = CreatQueryForRetrieveActiveCasesByCustomer(accountRef);
                var entityCollection = service.RetrieveMultiple(query);

                var cases = new List<Entity>();
                cases.AddRange(entityCollection.Entities.ToList());
                tracingService.Trace($"Cases retreived: {cases.Count}");

                //Resolve LAST case and save email to update account
                var caseToSolve = cases[0];
                string newMail = caseToSolve[NEW_TO_CHANGE_EMAIL].ToString();
                ResolveCase(service, caseToSolve);

                tracingService.Trace($"Case Id: {caseToSolve.Id} resolved");

                //Close all other cases
                for (int i = 1; i < cases.Count; i++)
                {                   
                    var currentCase = cases[i];
                    CancelCase(service, currentCase);

                    tracingService.Trace($"Case Id: {currentCase.Id} closed!");
                }
                #endregion

                #region Update Account email
                
                var customer = service.Retrieve(ACCOUNT, accountRef.Id, new ColumnSet(EMAIL_ATTRIBUTE));
                customer.Attributes[EMAIL_ATTRIBUTE] = newMail;
                service.Update(customer);
                tracingService.Trace($"New email: {newMail} was saved in customer account");
                #endregion

                //Email to customer implemented with additional step in workflow
            }
            catch (Exception ex)
            {
                tracingService.Trace($@"Exception thrown Message: {ex.Message} 
                                        {Environment.NewLine} StackTrace: {ex.StackTrace}
                                        {Environment.NewLine} Inner Exception: {ex.InnerException}");
                throw new Exception(ex.Message);
            }            
        }

        private static void CancelCase(IOrganizationService service, Entity currentCase)
        {
            currentCase[NEW_CHANGE_EMAIL_STATUS] = new OptionSetValue(100000003); //Declined status of ChangeEmailStatus field
            currentCase[NEW_CHANGE_EMAIL_STATUS_REASON] = new OptionSetValue(100000001);//Cancelled-Duplicated record
            service.Update(currentCase);
            //IS Update needed?

            SetStateRequest request = new SetStateRequest()
            {
                EntityMoniker = new EntityReference(INCIDENT, currentCase.Id),
                State = new OptionSetValue(2), //Cancell Cace(incidend)
                Status = new OptionSetValue(6)
            };
            service.Execute(request);
        }

        private static void ResolveCase(IOrganizationService service, Entity caseToSolve)
        {
            caseToSolve[NEW_CHANGE_EMAIL_STATUS] = new OptionSetValue(100000004); //Approved status of ChangeEmailStatus field
            caseToSolve[NEW_CHANGE_EMAIL_STATUS_REASON] = new OptionSetValue(100000002);//Approved - valid request
            service.Update(caseToSolve);
            //IS update needed?


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
        }

        private static QueryExpression CreatQueryForRetrieveActiveCasesByCustomer(EntityReference customer)
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
            query.Criteria.AddCondition(new ConditionExpression(TITLE, ConditionOperator.Equal, EMAIL_CHANGE));

            query.Criteria.FilterOperator = LogicalOperator.And;
            query.Orders.Add(new OrderExpression(CREATED_ON, OrderType.Descending));

            return query;
        }

        private void DeleteAllTracesForThisPlugIn(IOrganizationService service)
        {
            QueryExpression query = CreateQueryForTracesByName(WORKFLOW_NAME);

            var tracesToDelete = service.RetrieveMultiple(query);
            foreach (var trace in tracesToDelete.Entities)
            {
                service.Delete(TRACE_LOG_ENTITY, trace.Id);
            }
        }

        private QueryExpression CreateQueryForTracesByName(string workFlowName)
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
