using System;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Account.PlugIns
{
    public class OnNameOrWebsiteUpdate_ComposeFullName : IPlugin
    {
        private readonly string preImageAlias = "Pre";
        private readonly string postImageAlias = "Post";

        public OnNameOrWebsiteUpdate_ComposeFullName()
        {
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth >= 2)
            {
                return;
            }
            try
            {
                tracingService.Trace($"Started at: {DateTime.Now}");

                Entity preImageEntity = (context.PreEntityImages != null 
                                        && context.PreEntityImages.Contains(this.preImageAlias)) 
                                                ? context.PreEntityImages[this.preImageAlias] 
                                                : null;
                tracingService.Trace(preImageEntity.LogicalName + string.Join(" ", preImageEntity.Attributes.Keys));

                Entity postImageEntity = (context.PostEntityImages != null 
                                         && context.PostEntityImages.Contains(this.postImageAlias)) 
                                                ? context.PostEntityImages[this.postImageAlias] 
                                                : null;

                tracingService.Trace(postImageEntity.LogicalName + string.Join(" ", postImageEntity.Attributes.Keys));

                //throw new InvalidPluginExecutionException(string.Format("preName: {0}", postName));


                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {

                    // Obtain the target entity from the input parameters.
                    Entity entity = (Entity)context.InputParameters["Target"];

                    tracingService.Trace(entity.LogicalName + " Id:" + entity.Id);

                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider
                        .GetService(typeof(IOrganizationServiceFactory));

                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    

                    var preImageName = preImageEntity.GetAttributeValue<string>("name");
                    var postImageName = postImageEntity.GetAttributeValue<string>("name");

                    tracingService.Trace($"Pre name: {preImageName}");
                    tracingService.Trace($"Poist name: {postImageName}");

                    var preImageUrl = preImageEntity.GetAttributeValue<string>("websiteurl");
                    var postImageUrl = postImageEntity.GetAttributeValue<string>("websiteurl");

                    tracingService.Trace($"Pre url: {preImageUrl}");
                    tracingService.Trace($"Post url: {postImageUrl}");



                    if (preImageName == postImageName
                        && preImageUrl == postImageUrl)
                    {
                        //No changes were made
                        return;
                    }

                    var accountGuid = entity.Id;
                    Entity accountObject = service.Retrieve(context.PrimaryEntityName, accountGuid, new ColumnSet(true));
                    //tracingService.Trace("concat current value: " + accountObject.Attributes["new_concatwithimage"]);

                    accountObject["new_concatwithimage"] = $"{postImageName} -UsingImages- {postImageUrl}";
                    // Create the followup activity

                    service.Update(accountObject);

                    tracingService.Trace("Successfull end"+ accountObject["new_concatwithimage"].ToString());
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Exception: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
