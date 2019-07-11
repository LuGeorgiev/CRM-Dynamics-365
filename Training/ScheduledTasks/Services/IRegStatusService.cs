using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface IRegStatusService
    {
        Guid GetStatusId(string registrationStatus);

        Guid GetSubStatusId(string registrationSubStatus);
    }
}
