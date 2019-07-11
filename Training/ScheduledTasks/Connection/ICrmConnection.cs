using System;

using Microsoft.Xrm.Sdk;
using XrmContext;

namespace Connection
{
    public interface ICrmConnection
    {
        bool IsConnected { get; }

        Guid GetUserId { get; }

        IOrganizationService Service { get; }

        XrmServiceContext GetContext();
    }
}
