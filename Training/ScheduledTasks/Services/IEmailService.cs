using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface IEmailService
    {
        bool SentRegistrationReport(IEnumerable<string> reportingEmail, IEnumerable<string> successfulOperations, IEnumerable<string> failedOperation);

        bool SentInformation(Guid toContactGuid);
    }
}
