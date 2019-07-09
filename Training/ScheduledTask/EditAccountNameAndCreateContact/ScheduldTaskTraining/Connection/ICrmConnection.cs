using Microsoft.Xrm.Sdk;
using System;

namespace ScheduldTaskTraining.Connection
{
    public interface ICrmConnection
    {
        bool IsConnected { get; }

        Guid GetUserId { get; }

        IOrganizationService Connect();
    }
}
