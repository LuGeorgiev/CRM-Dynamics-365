using System;
using System.Collections.Generic;
using System.Linq;
using Connection;
using log4net;
using Models.Account;
using Models.Case;
using Models.Contact;
using Models.Registration;

using static Models.Constats;

namespace Services.Implementation
{
    public class ContactService : IContactService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICrmConnection crm;
        private readonly IRegStatusService regStatusService;

        public ContactService(ICrmConnection crmConnection, IRegStatusService regStatusService)
        {
            this.crm = crmConnection;
            this.regStatusService = regStatusService;
        }

        public IEnumerable<ContactRetrieveModel> AllContactsWithRegistrationInAccount(string registrationStatus, string registeredSubStatus)
        {
            log.Info($"Retrieve Acontacts that have account with {registrationStatus} status in registartion and {registeredSubStatus} in subStatus.");

            try
            {
                using (var context = this.crm.GetContext())
                {
                    //Retreive Guid of Registration with status Given Status (OPEN)
                    var registrationStatusId = regStatusService.GetStatusId(registrationStatus);

                    //Retreive Guid of Registration with SUB status Given Status (Registered)
                    var registrationSubStatusId = regStatusService.GetSubStatusId(registeredSubStatus);

                    if (registrationStatusId == null || registrationSubStatusId == null)
                    {
                        log.Error($"No such status exist. Will return");
                    }

                    //Retreive Contacts with Accounts and All Registrations that fit the condition
                    var contacts = (from account in context.AccountSet
                                    join contact in context.ContactSet
                                         on account.PrimaryContactId.Id equals contact.ContactId
                                    join registration in context.new_registrationSet
                                         on account.AccountId equals registration.new_account.Id
                                    where registration.new_registraionstatus.Id == registrationStatusId
                                            && registration.new_registrationsubstatus.Id == registrationSubStatusId
                                    select new ContactRetrieveModel
                                    {
                                        Id = contact.ContactId.ToString(),
                                        LastName = contact.LastName,
                                        Email = contact.EMailAddress1,
                                        Account = new AccountRetreiveModel
                                        {
                                            Id = account.AccountId.ToString(),
                                            Name = account.Name,
                                            Registrations = (from acc in context.AccountSet
                                                             join reg in context.new_registrationSet
                                                                on acc.AccountId equals reg.new_account.Id
                                                             where acc.AccountId == account.AccountId
                                                             select new RegistrationRetreiveModel
                                                             {
                                                                 Id = reg.new_registrationId.ToString(),
                                                                 Name = reg.new_name,
                                                                 Priority = reg.new_priority ?? -1,
                                                                 CreatedOn = reg.CreatedOn.Value
                                                             })
                                                             .ToList()
                                        }
                                    })
                                    .Distinct()
                                    .ToList();
                    if (contacts == null || contacts.Count == 0)
                    {
                        log.Info($"No Contacts were retreived");
                    }
                    else
                    {
                        log.Info($"Number of Contacts retreived {contacts.Count()}. Registrations retrieved: {contacts.SelectMany(x=>x.Account.Registrations).Count()}");
                    }

                    return contacts;
                }
                
            }
            catch (Exception ex)
            {
                log.Error($"{nameof(AllContactsWithRegistrationInAccount)} throws exception: {ex.Message}");
                Console.WriteLine(ex.Message);

                return Enumerable.Empty<ContactRetrieveModel>();
            } 

        }
    }
}
