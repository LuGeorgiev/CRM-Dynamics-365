using StudentManagment.Data;
using StudentManagment.Data.Implementation;
using StudentManagment.Services;
using StudentManagment.Services.Implementation;

using System.Web.Mvc;
using Unity;
using Unity.Injection;
using Unity.Mvc5;

namespace StudentManagment.Web
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            // register all your components with the container here
            // it is NOT necessary to register your controllers
            

            container.RegisterSingleton<IDbConnection,CrmConnection>(new InjectionFactory(c=> new CrmConnection(
                                                                                System.Configuration.ConfigurationManager.AppSettings["username"], 
                                                                                System.Configuration.ConfigurationManager.AppSettings["password"],
                                                                                System.Configuration.ConfigurationManager.AppSettings["crmUri"])));

            //Following Db Registration to be used if Credentials form AppSettings are NOT used
            //NOT Needed now Credentials comes from AppSettings
            //container.RegisterSingleton<IDbConnection, CrmConnection>();

            container.RegisterType<IStudentService, StudentService>();
            container.RegisterType<ICaseService, CaseService>();
            
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}