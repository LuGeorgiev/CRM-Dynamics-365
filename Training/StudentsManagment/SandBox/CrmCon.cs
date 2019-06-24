using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel.Description;

namespace SandBox
{
    class CrmCon
    {
        private IOrganizationService organizationService = null;
        private Guid userid;


        public CrmCon()
        {            

            Debug.WriteLine("Crm Connection started");
            try
            {

                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = Credentials.username;
                clientCredentials.UserName.Password = Credentials.password;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                OrganizationServiceProxy proxy = new OrganizationServiceProxy(
                    new Uri(Credentials.crmUri),
                    null, 
                    clientCredentials, 
                    null);

                proxy.EnableProxyTypes();
                organizationService = (IOrganizationService)proxy;

                if (organizationService != null)
                {
                    var user = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest()));
                    userid = user.UserId;

                    Debug.WriteLine("Connection Established Successfully...");

                    if (userid != Guid.Empty)
                    {
                        Console.WriteLine($"Connected Userid: {this.userid}");
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to Established Connection!!!");
                    throw new Exception("Connection NOT established");
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught - " + ex.Message);
                throw new Exception("Connection NOT established");

            }
        }

        public Guid GetUserId
            => this.userid;

        public IOrganizationService Connect()
            => this.organizationService;

        public bool IsConnected
            => organizationService != null;
    }
}

