using System;
using System.Linq;

using ScheduldTaskTraining.Connection.Implmentation;
using ScheduldTaskTraining.Connection;
using ScheduldTaskTraining.Services;
using ScheduldTaskTraining.Services.Implementation;
using ScheduldTaskTraining.Models.Account;

using log4net;

using static ScheduldTaskTraining.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace ScheduldTaskTraining
{
    class Program
    {
        //private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            log.Info("Main Start");
            IServiceCollection serviceCollection = CreateCollection();
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            
            IEngine engine = serviceProvider.GetService<IEngine>();
            engine.Run();        
            
        }

        private static IServiceCollection CreateCollection()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<ICrmConnection, CrmConnection>();
            serviceCollection.AddScoped<IContactService, ContactService>();
            serviceCollection.AddScoped<IAccountService, AccountService>();
            serviceCollection.AddScoped<IEngine, Engine>();

            log.Info("Service collection created successfully");
            return serviceCollection;
        }

        
    }
}
