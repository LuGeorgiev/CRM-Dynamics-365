using System;
using System.Net;
using System.ServiceModel.Description;

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;

using log4net;
using XrmContext;

namespace Connection.Implementation
{
    public class CrmConnection : ICrmConnection
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CrmConnection));

        private IOrganizationService organizationService = null;
        private Guid userid = Guid.Empty;
        //private XrmServiceContext xrmContext = null;

        public CrmConnection(string username, string password, string crmUri)
        {
            log.Info("Crm Connection started");
            try
            {    
                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = username;
                clientCredentials.UserName.Password = password;                

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                OrganizationServiceProxy proxy = new OrganizationServiceProxy(new Uri(crmUri), null, clientCredentials, null);

                proxy.EnableProxyTypes();
                this.organizationService = (IOrganizationService)proxy;
                //this.xrmContext = new XrmServiceContext(this.organizationService);

                if (this.organizationService != null)
                {
                    userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;
                    Console.WriteLine("Connection Established Successfully...");
                    log.Info("Connection Established Successfully...");

                    if (userid != Guid.Empty)
                    {
                        log.Info($"Connected Userid: {this.userid}");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to Established Connection!!!");
                    log.Fatal("Failed to Established Connection!!!");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught - " + ex.Message);
                log.Error($"Exception caught - {ex.Message}");
            }
        }

        public CrmConnection(string crmServiceConnectionString)
        {
            log.Info("Crm Connection started");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CrmServiceClient organizationServiceTwo = null;

            try
            {
                organizationServiceTwo = new CrmServiceClient(crmServiceConnectionString);
                userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;

                if (organizationServiceTwo.IsReady == true && userid != Guid.Empty)
                {
                    Console.WriteLine("Connection Established Successfully...");
                    log.Info($"Connection Established Successfully! Userid: {this.userid}");
                }
                else
                {
                    Console.WriteLine("Connection NOT Established");
                    return;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error detected while connecting with CrmServiceClient - {ex.Message}");
            }

            organizationService = organizationServiceTwo;
        }

        public Guid GetUserId
            => this.userid;

        public IOrganizationService Service
            => this.organizationService;

        public bool IsConnected
            => this.organizationService != null;

        public XrmServiceContext GetContext()
            => new XrmServiceContext(this.organizationService);
    }
}
