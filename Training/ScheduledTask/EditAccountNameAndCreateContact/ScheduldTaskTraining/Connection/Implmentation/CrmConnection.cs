using System;
using System.Configuration;
using System.Net;
using System.ServiceModel.Description;

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;

using log4net;

namespace ScheduldTaskTraining.Connection.Implmentation
{
    public class CrmConnection :ICrmConnection
    {
        //private static readonly log4net.ILog log = log4net
        //    .LogManager
        //    .GetLogger(System
        //        .Reflection
        //        .MethodBase
        //        .GetCurrentMethod()
        //        .DeclaringType);

        private static readonly ILog log = LogManager.GetLogger(typeof(CrmConnection));

        private IOrganizationService organizationService = null;
        private Guid userid;

        public CrmConnection()
        {
            log.Info("Crm Connection started");
            try
            {

                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = ConfigurationManager.AppSettings["username"];
                clientCredentials.UserName.Password = ConfigurationManager.AppSettings["password"];
                var crmUri = ConfigurationManager.AppSettings["crmUri"];

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                OrganizationServiceProxy proxy = new OrganizationServiceProxy(new Uri(crmUri), null, clientCredentials, null);

                proxy.EnableProxyTypes();
                organizationService = (IOrganizationService)proxy;

                if (organizationService != null)
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

        public IOrganizationService Connect()
            => this.organizationService;

        public bool IsConnected
            => organizationService != null;
    }
}
