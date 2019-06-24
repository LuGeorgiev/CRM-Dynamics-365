using Microsoft.Xrm.Sdk;
using System;

namespace StudentManagment.Data
{
    public interface IDbConnection
    {
        bool IsConnected { get; }

        Guid GetUserId { get; }

        IOrganizationService Connect();
    }
}
