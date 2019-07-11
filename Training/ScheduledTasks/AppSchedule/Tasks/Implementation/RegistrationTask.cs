using log4net;
using Models.Registration;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;



using static Models.Constats;

namespace AppSchedule.Tasks.Implementation
{
    public class RegistrationTask : IRegistrationTask
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IContactService contactService;
        private readonly ICaseService caseService;
        private readonly IRegistrationService registrationService;
        private readonly IEmailService emailService;
        private readonly IRegStatusService regStatusService;
        private readonly ISystemRuleService systemRulesService;

        //Constructor injection all services. One more and refactor to separete functionallity will be needed
        public RegistrationTask(IContactService contactService, 
            ICaseService caseService, 
            IRegistrationService registrationService, 
            IEmailService emailService, 
            IRegStatusService regStatusService,
            ISystemRuleService systemRulesService)
        {
            this.contactService = contactService;
            this.caseService = caseService;
            this.registrationService = registrationService;
            this.emailService = emailService;
            this.regStatusService = regStatusService;
            this.systemRulesService = systemRulesService;
        }

        //TODO Delete this method when finish
        public void TestMethod()
        {
            var mails = new List<string>() { "lujo@kol.io", "maikamu@ko.lop" };
            var succes = new List<string>() { "first~second~third", "one~two~three", "213~sdfdsf~sdfsffhh" };
            var fail = new List<string>() { "first~second~1212", "one~two~23234", "213~sdfdsf~45677" };

            emailService.SentRegistrationReport(mails, succes, fail);
            //retreive RegistrationStatus Cancelled
            //var cancelStatusId = this.regStatusService.GetStatusId(CANCELLED_REGISTRATION_STATUS);

            ////retreive RegistrationSubStatus Cancelled by task
            //var canceledSubStatusId = this.regStatusService.GetSubStatusId(CANCELLED_REGISTRATION_SUB_STATUS);

            //var regToUpdate = registrationService.CancellByTask(Guid.Empty.ToString(), cancelStatusId, canceledSubStatusId);
        }

        public bool? Execute()
        {           

            log.Info($"Start executing Registration task");

            try
            {
                //Retreive Contacts with corresponding accounts and Registrations embedde
                var contacts = contactService
                    .AllContactsWithRegistrationInAccount(OPEN_REGISTRATION_STATUS,
                                                          REGISTERD_REGISTRATION_SUB_STATUS);
                if (contacts == null || contacts.Count() == 0)
                {
                    log.Info("No contacts with open registrations found");
                    return false;
                }

                //Create unique accountsId collection
                HashSet<string> accountIds = new HashSet<string>();
                foreach (var contact in contacts)
                {
                    accountIds.Add(contact.Account.Id);
                }

                //Retreive caseses that satisfys the conditions
                var cases = caseService
                    .AllActiveByTypeAndAccountId(accountIds,
                                                 CASE_TYPE_REQUEST,
                                                 CASE_TITLE_CANCEL_REGISTRATION);
                if (cases == null || cases.Count() == 0)
                {
                    log.Info("No Cases with open registrations found");
                    return false;
                }

                //Create new Collection of all retrieved Registrations
                HashSet<RegistrationRetreiveModel> registrations =
                    new HashSet<RegistrationRetreiveModel>(contacts.SelectMany(x => x.Account.Registrations));

                //retreive RegistrationStatus Cancelled
                var cancelStatusId = this.regStatusService
                    .GetStatusId(CANCELLED_REGISTRATION_STATUS);

                //retreive RegistrationSubStatus Cancelled by task
                var canceledSubStatusId = this.regStatusService
                    .GetSubStatusId(CANCELLED_REGISTRATION_SUB_STATUS);

                if (canceledSubStatusId == Guid.Empty
                    || cancelStatusId == Guid.Empty)
                {
                    log.Error("Statuses were not retrieved correctly");

                    return false;
                }

                var failedOperations = new List<string>();
                var successfulOperations = new List<string>();

                //For each case found Update a registration with the lowest priority, if two registrations found with the same priority take the last created one 
                foreach (var currentCase in cases)
                {
                    var accountName = contacts
                        .FirstOrDefault(x => x.Account.Id == currentCase.CaseCustomer)
                        .Account
                        .Name;
                    var caseName = currentCase
                        .Title;

                    log.Info($"Working on Case with id: {currentCase.Id}");

                    //query the registration to update
                    var regToUpdateId = contacts
                        .Select(con => con.Account)
                        .Where(acc => acc.Id == currentCase.CaseCustomer) //Filter only accounts that are for this particular Case
                        .SelectMany(acc => acc.Registrations)             //Take only RegistrationS from account
                        .OrderByDescending(reg => reg.Priority)           //Order
                        .ThenByDescending(reg => reg.CreatedOn)
                        .FirstOrDefault();                                //Take just one

                    var regToUpdateName = regToUpdateId
                        .Name;

                    #region update registration and close case
                    //Update Task
                    var updateResult = this.registrationService
                        .CancellByTask(regToUpdateId.Id, cancelStatusId, canceledSubStatusId);
                    if (updateResult)
                    { 
                        log.Info($"Task updated successfully");
                    }
                    else
                    {
                        failedOperations.Add($"{accountName}{SEPARATOR}{regToUpdateName}{SEPARATOR}Registration was NOT updated successfully");
                        log.Error($"Issue occured and not updated");
                    }

                    //Close case
                    var closeCaseResult = this.caseService
                        .ResolveCase(currentCase.Id);
                    if (closeCaseResult)
                    {
                        log.Info($"Case closed successfully");
                    }
                    else
                    {
                        failedOperations.Add($"{accountName}{SEPARATOR}{regToUpdateId}{SEPARATOR}Case was NOT closed successfully");
                        log.Error($"Issue occured case was not closed");
                    }
                    #endregion

                    #region Email to customer sending

                    //Query the contact to which the INFO mail have to be sent
                    var contactToSent = contacts
                        .FirstOrDefault(x => x.Account.Name == accountName).Id;

                    //TODO
                    //Email sending
                    var isEmailSent = emailService.SentInformation(Guid.Parse(contactToSent));
                    if (! isEmailSent)
                    {
                        failedOperations.Add($"{accountName}{SEPARATOR}{regToUpdateName}{SEPARATOR}Email was NOT successfully sent!");
                        log.Error("To Email was not retrieved");
                    }

                    #endregion

                    successfulOperations.Add($"{accountName}{SEPARATOR}{caseName}{SEPARATOR}{regToUpdateName}{SEPARATOR}All operations were successfull!");
                }

                var reportingEmail = this.systemRulesService.AllRegistrationReportEmails();
                bool wereSent = emailService.SentRegistrationReport(reportingEmail, successfulOperations, failedOperations);
                if (! wereSent)
                {
                    log.Error("Reporting emails were NOT sent successfully!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"{nameof(Execute)} throws exception: {ex.Message}");
                Console.WriteLine(ex.Message);

                return null;
            }

            
        }
                
    }
}
