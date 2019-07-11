using Models.Registration;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using static Models.Constats;
using AppSchedule.Tasks;

namespace AppSchedule
{
    // NB Class Engine have to be refactored to execute different tasks and teh logic to be in TASKS classes
    class Engine : IEngine
    {

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISystemRuleService systemRuleService;
        private readonly IRegistrationTask registrationTask;

        public Engine(ISystemRuleService systemRuleService, IRegistrationTask registrationTask)
        {
            this.systemRuleService = systemRuleService;
            this.registrationTask = registrationTask;
        }

        public void Run()
        {
            log.Info($"Start running");

            registrationTask.TestMethod();

            //Retreive date on which Registration task should run
            var runDateRegistrationTask = systemRuleService.GetRegistrationTaskDay();

            // Check if Task have to run today
            if (runDateRegistrationTask != null 
                && runDateRegistrationTask.Value.Date == DateTime.UtcNow.Date)
            {
                //this.registrationTask.TestMethod();

                var result = this.registrationTask.Execute();

                //TODO LOG depending on result
            }            
        }
    }
}
