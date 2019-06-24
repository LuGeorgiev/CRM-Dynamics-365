using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel.Description;

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;


namespace StudentManagment.Data.Implementation
{
    public class CrmConnection : IDbConnection
    {
        private IOrganizationService organizationService = null;
        private Guid userid;

        public CrmConnection(string username, string password, string crmUri)
        {
            Debug.WriteLine("Crm Connection started");

            try
            {

                ClientCredentials clientCredentials = new ClientCredentials();

                clientCredentials.UserName.UserName = username;
                clientCredentials.UserName.Password = password;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                OrganizationServiceProxy proxy = new OrganizationServiceProxy(new Uri(crmUri), null, clientCredentials, null);

                proxy.EnableProxyTypes();
                organizationService = (IOrganizationService)proxy;

                if (organizationService != null)
                {
                    userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;
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

        #region PreviousConstructors

        //public CrmConnection()
        //{
        //    Debug.WriteLine("Crm Connection started");

        //    try
        //    {              

        //        ClientCredentials clientCredentials = new ClientCredentials();

        //        clientCredentials.UserName.UserName = Credentials.username;
        //        clientCredentials.UserName.Password = Credentials.password;                                                                    

        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //        OrganizationServiceProxy proxy = new OrganizationServiceProxy(new Uri( Credentials.crmUri), null, clientCredentials, null);

        //        proxy.EnableProxyTypes();
        //        organizationService = (IOrganizationService)proxy;

        //        if (organizationService != null)
        //        {
        //            userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;
        //            Debug.WriteLine("Connection Established Successfully...");

        //            if (userid != Guid.Empty)
        //            {
        //                Console.WriteLine($"Connected Userid: {this.userid}");
        //            }
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Failed to Established Connection!!!");
        //                throw new Exception("Connection NOT established");
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Exception caught - " + ex.Message);
        //        throw new Exception("Connection NOT established");

        //    }
        //}



        //public CrmConnection(string crmServiceConnectionString)
        //{
        //    Debug.WriteLine("Crm Connection started");

        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //    CrmServiceClient organizationServiceTwo = null;

        //    try
        //    {
        //        organizationServiceTwo = new CrmServiceClient(crmServiceConnectionString);
        //        userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;

        //        if (organizationServiceTwo.IsReady == true && userid != Guid.Empty)
        //        {
        //            Debug.WriteLine("Connection Established Successfully...");
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Connection NOT Established");
        //            return;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error detected while connecting with CrmServiceClient - {ex.Message}");
        //    }

        //    organizationService = organizationServiceTwo;
        //}
        #endregion


    }
}
