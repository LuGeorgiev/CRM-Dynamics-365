using Connection;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{   

    public interface IRegistrationService
    {
        bool CancellByTask(string registrationId, Guid cancelStatusId, Guid canceledSubStatusId);
    }
}
