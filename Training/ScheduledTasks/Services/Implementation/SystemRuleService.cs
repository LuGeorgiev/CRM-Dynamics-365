using Connection;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Models.Constats;

namespace Services.Implementation
{
    public class SystemRuleService : ISystemRuleService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICrmConnection crmConnection;

        public SystemRuleService(ICrmConnection crmConnection)
        {
            this.crmConnection = crmConnection;
        }

        public IEnumerable<string> AllRegistrationReportEmails()
        {
            var result = string.Empty;
            using (var context = crmConnection.GetContext())
            {
                result = context.new_systemrulesSet
                    .FirstOrDefault(x => x.new_Slug == REGISTRATION_TASK_OWNERS_SLUG)
                    .new_RuleValue;
            }

            if (string.IsNullOrEmpty(result))
            {
                log.Error("Owners mails faild to retreive");
                return Enumerable.Empty<string>();
            }

            log.Info($"E-mails were retreived");
            return result.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        public DateTime? GetRegistrationTaskDay()
        {
            DateTime? result = null;

            using (var context = crmConnection.GetContext())
            {
                DateTime.TryParse(context.new_systemrulesSet
                    .FirstOrDefault(x => x.new_Slug == REGISTRATION_TASK_DATE_SLUG)
                    .new_RuleValue, out var date);

                result = date;
            }

            if (result == null)
            {
                log.Error("Registration task date was not retreived");
                return null;
            }

            log.Info($"Date to run is {result.Value}");
            return result;
        }
    }
}
