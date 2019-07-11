using log4net;
using System;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Services;
using Services.Implementation;
using Connection;
using Connection.Implementation;

using static Models.Constats;
using AppSchedule.Tasks;
using AppSchedule.Tasks.Implementation;

namespace AppSchedule
{
    class StartUp
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            log.Info("Program strated.");
            IServiceCollection serviceCollection = CreateCollection();
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            IEngine engine = serviceProvider.GetService<IEngine>();
            engine.Run();

        }

        private static IServiceCollection CreateCollection()
        {
            var serviceCollection = new ServiceCollection();
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            var crmUri = ConfigurationManager.AppSettings["crmUri"];
            log.Info($"Connection will be created for user: {username} and Crm instance: {crmUri}");

            //Registed Singleton CRM connection
            serviceCollection.AddSingleton<ICrmConnection>(new CrmConnection(username,password,crmUri));

            //Register utilities services
            serviceCollection.AddScoped<ICaseService, CaseService>();
            serviceCollection.AddScoped<IEmailService, EmailService>();
            serviceCollection.AddScoped<ISystemRuleService, SystemRuleService>();
            serviceCollection.AddScoped<IRegistrationService, RegistrationService>();
            serviceCollection.AddScoped<IContactService, ContactService>();
            serviceCollection.AddScoped<IRegStatusService, RegStatusService>();

            //Register Buisness logic services
            serviceCollection.AddTransient<IEngine, Engine>();
            serviceCollection.AddTransient<IRegistrationTask, RegistrationTask>();

            log.Info("Service collection created successfully");
            return serviceCollection;
        }
    }
}
