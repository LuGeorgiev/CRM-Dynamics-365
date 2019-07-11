using Connection;
using log4net;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using XrmContext;

using static Models.Constats;

namespace Services.Implementation
{
    public class RegistrationService : IRegistrationService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICrmConnection crmConnection;
        private readonly IRegStatusService regStatusService;

        public RegistrationService(ICrmConnection crmConnection, IRegStatusService regStatusService)
        {
            this.crmConnection = crmConnection;
            this.regStatusService = regStatusService;
        }

        public bool CancellByTask(string registrationId, Guid cancelStatusId, Guid canceledSubStatusId)
        {
            log.Info($"Registration to cancel Id: {registrationId}");
            try
            {      
                var regToUpdate = new new_registration();
                regToUpdate.Id = Guid.Parse(registrationId);

                //Set RegistrationStatuses
                regToUpdate.new_registraionstatus = new EntityReference(new_registrationstatus.EntityLogicalName, cancelStatusId);
                regToUpdate.new_registrationsubstatus = new EntityReference(new_registrationsubstatus.EntityLogicalName, canceledSubStatusId);

                crmConnection.Service.Update(regToUpdate);
                log.Info("Updatet sucessfully");

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"{nameof(CancellByTask)} throws exception: {ex.Message}");
                Console.WriteLine(ex.Message);

                return false;
            }
        }
    }
}
