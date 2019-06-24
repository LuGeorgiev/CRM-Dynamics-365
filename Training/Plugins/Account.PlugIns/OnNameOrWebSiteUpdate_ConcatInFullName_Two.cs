using System;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Account.PlugIns
{
    // OnPostOr On PRe Action(update create etc.)_ConcatenateName (the JOB)
    public class OnNameOrWebSiteUpdate_ConcatInFullName_Two: IPlugin
    {        

        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            OrganizationServiceContext orgContext = new OrganizationServiceContext(service);

            if (context.Depth >1)
            {
                return;
            }

            try
            {                

                Entity target = null;
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    target = context.InputParameters["Target"] as Entity;

                    if (target != null && context.MessageName.ToLower()=="update")
                    {
                        target = service.Retrieve("account", target.Id, new ColumnSet(new[] {"name", "websiteurl", "new_lubofullname" })); // ONLY needed fields
                    }

                    if (target==null)
                    {
                        return;
                    }

                    string name = target.Attributes["name"] == null 
                        ? ""
                        : target.Attributes["name"].ToString();

                    string url = target.Attributes["websiteurl"] == null 
                        ? "Site url was NOT filled"
                        : target.Attributes["websiteurl"].ToString();

                    target["new_lubofullname"] = $"{name} - {url}"; //Pre EVENT NO NEED for service UPDATE

                    service.Update(target); 
                    // All fieklds will be UPDATED because of TRUE in Colums
                    // A lot of unneeded changes
                    //SELECTIVE UPDATE (only changed updates) NOTHA BENNE
                    
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
