using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using System;

namespace AccountCase.SecondTask.PlugIns
{
    public class PreAccountEmailChange_CreateCaseKeepOldEmail : IPlugin
    {
        //Image constants
        private readonly string preImageAlias = "PreImage";
        private readonly string postImageAlias = "PostImage";

        //Plugin constants
        private const string TARGET_ENTITY = "Target";
        private const string EMAIL_ATTRIBUTE = "emailaddress1";
        private const string NOT_AVAILABLE = "Not Available";
        private readonly Guid SUBJECT_EMAIL_CHANGE = Guid.Parse("D5911B1A-DA8D-E911-A81B-000D3ABA3097");

        //Delete Plugin trace constants
        private const string PLUGIN_TRACELOG_NAME = "PluginProfiler.Plugins.ProfilerPlugin";
        private const string TRACE_LOG_ENTITY = "plugintracelog";
        private const string TYPE_NAME = "typename";


        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth >= 2)
            {
                tracingService.Trace("Depth of plug in PreAccountEmailChange_CreateCaseKeepOldEmail more than 2 return");
                return;
            }

            try
            {
                //NB
                //Check for context.Message== UPDATE


                // Delete All traces for thise plugin
                DeleteAllTracesForThisPlugIn(service);
                tracingService.Trace("Start of plug in PreAccountEmailChange_CreateCaseKeepOldEmail");

                //Images retrieval
                Entity preImageEntity = (context.PreEntityImages != null && context.PreEntityImages.Contains(this.preImageAlias))
                                                ? context.PreEntityImages[this.preImageAlias]
                                                : null;
                Entity postImageEntity = (context.PostEntityImages != null && context.PostEntityImages.Contains(this.postImageAlias))
                                                ? context.PostEntityImages[this.postImageAlias]
                                                : null;

                //if executing user and the modified by are same return;
                var postModifiedBy = postImageEntity.GetAttributeValue<EntityReference>("modifiedby").Id;

                //executing user
                WhoAmIRequest systemUserRequest = new WhoAmIRequest();
                WhoAmIResponse systemUserResponse = (WhoAmIResponse)service.Execute(systemUserRequest);
                var executingUser = systemUserResponse.UserId;
                if (postModifiedBy == executingUser)
                {
                    tracingService.Trace($"Executing user and modifiedBy user are same");
                    return;
                }


                Guid? targetId = null;
                Entity target = null;

                if (context.InputParameters.Contains(TARGET_ENTITY) && context.InputParameters[TARGET_ENTITY] is Entity)
                {
                    target = (Entity)context.InputParameters[TARGET_ENTITY];
                    tracingService.Trace($"Entity {target.LogicalName} with id: {target.Id} retrieved");

                    targetId = target.Id;
                }

                if (service == null)
                {
                    tracingService.Trace("IServiceOrganization is null");
                    return;
                }

                var preImageEmail = preImageEntity.GetAttributeValue<string>(EMAIL_ATTRIBUTE) ?? NOT_AVAILABLE;
                var postImageEmail = postImageEntity.GetAttributeValue<string>(EMAIL_ATTRIBUTE) ?? NOT_AVAILABLE;

                tracingService.Trace($"PreImage email: { preImageEmail }");
                tracingService.Trace($"PostImage email: { postImageEmail }");

                if (preImageEmail == postImageEmail
                    || targetId == null
                    || postImageEmail == NOT_AVAILABLE)
                {
                    tracingService.Trace($"Old mail: {preImageEmail} new mail: {postImageEmail} exit from pluging due to not changed or target entity id equals null");
                    return;
                }

                Entity newCaseEntity = CreateCase(service, targetId.Value, preImageEmail, postImageEmail);
                tracingService.Trace($"Case to create");

                service.Create(newCaseEntity);

                tracingService.Trace("Case created. To Save old mail value");
                target[EMAIL_ATTRIBUTE] = preImageEmail;
                service.Update(target);

                tracingService.Trace("Sucessfully saved");
            }
            catch (InvalidPluginExecutionException ex)
            {
                tracingService.Trace($"Exception: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private Entity CreateCase(IOrganizationService service, Guid targetId, string preImageEmail, string postImageEmail)
        {
            Entity incident = new Entity();

            incident.LogicalName = "incident";
            incident["title"] = "Email change";
            incident["description"] = "This is Email change request.";
            incident["customerid"] = new EntityReference("account", targetId);
            incident["new_changeemailstatus"] = new OptionSetValue(100000001); //InProgress ALL EXCEPTIONS AFTER THIS OptionsSet Included
            incident["new_previousemail"] = preImageEmail;
            incident["new_tochangeemail"] = postImageEmail;
            incident["subjectid"] = new EntityReference("subject", SUBJECT_EMAIL_CHANGE);

            //incident.Attributes.Add("customerid", new EntityReference("account", targetId));

            return incident;
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
