using Connection;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementation
{
    public class RegStatusService : IRegStatusService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICrmConnection crmConnection;

        public RegStatusService(ICrmConnection crmConnection)
        {
            this.crmConnection = crmConnection;
        }

        public Guid GetStatusId(string registrationStatus)
        {
            Guid registrationStatusId = Guid.Empty;

            using (var context = crmConnection.GetContext())
            {
                registrationStatusId = context.new_registrationstatusSet
                        .FirstOrDefault(x => x.new_name == registrationStatus).Id;
            }

            if (registrationStatusId == Guid.Empty)
            {
                log.Error("Registration Status Id was not retrieved");
            }
            else
            {
                log.Error("Registration Status Id was retrieved successfully");
            }

            return registrationStatusId;
        }

        public Guid GetSubStatusId(string registrationSubStatus)
        {
            Guid registrationSubStatusId = Guid.Empty;

            using (var context = crmConnection.GetContext())
            {
                registrationSubStatusId = context.new_registrationsubstatusSet
                    .FirstOrDefault(x => x.new_name == registrationSubStatus).Id;
            }

            if (registrationSubStatusId == Guid.Empty)
            {
                log.Error("Registration Status Id was not retrieved");
            }
            else
            {
                log.Error("Registration Status Id was retrieved successfully");
            }

            return registrationSubStatusId;
        }
    }
}
