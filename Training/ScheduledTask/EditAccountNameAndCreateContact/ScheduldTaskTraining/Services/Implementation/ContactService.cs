using log4net;
using Microsoft.Xrm.Sdk;
using ScheduldTaskTraining.Connection;
using System;

using XRM;

namespace ScheduldTaskTraining.Services.Implementation
{
    public class ContactService : IContactService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ContactService));

        private readonly IOrganizationService service;

        public ContactService(ICrmConnection connection)
        {
            this.service = connection.Connect();
        }

        //Create new Contact entity with specific input
        public Guid CreateContact(string email, string firstName, string lastName, string address)
        {
            log.Info($"New contact with first name: {firstName ?? "N/A"} last name: {lastName}, email: {email ?? "N/A"} and address: {address ?? "N/A"} will be created.");
            try
            {
                var newContact = new Contact();
                newContact.FirstName = firstName;
                newContact.LastName = lastName;
                newContact.Address1_Line1 = address;
                newContact.EMailAddress1 = email;

                return service.Create(newContact);             

            }
            catch (Exception ex)
            {
                log.Error($"Exception throw during contact creation: {ex.Message}");
                Console.WriteLine($"Exception throw during contact creation: {ex.Message}");

                return Guid.Empty;
            };
        }
    }
}
