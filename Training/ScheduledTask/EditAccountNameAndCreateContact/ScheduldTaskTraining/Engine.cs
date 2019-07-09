using System;
using System.Linq;
using log4net;
using ScheduldTaskTraining.Models.Account;
using ScheduldTaskTraining.Services;

using static ScheduldTaskTraining.Constants;

namespace ScheduldTaskTraining
{
    public class Engine : IEngine
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Engine));

        private readonly IAccountService accountService;
        private readonly IContactService contactService;

        public Engine(IAccountService accountService, IContactService contactService)
        {
            this.contactService = contactService;
            this.accountService = accountService;
        }

        //Buisness logic goes here
        public void Run()
        {
            log.Info("Engine will run....");

            //Retrieve Accounts that satisfy condition
            var accounts = accountService.AllContainingInName(TO_REPLACE);

            foreach (var account in accounts)
            {
                var accountToModify = new AccountWithNameModel
                {
                    Id = account.Id,
                    FullName = account.FullName
                };

                //Replace in name
                var changedName = accountService.ReplaceInName(accountToModify, TO_REPLACE, REPLACE_WITH);

                //If name was not changed new contact will not be created
                if (string.IsNullOrEmpty(changedName))
                {
                    //LOG ERROR
                    log.Info("Name was not edited successfully!");
                    continue;
                }

                //From full name split Last name and all othe to be First name
                SplitName(changedName, out string firstName, out string lastName);

                //Creat new contact
                var newContactId = contactService.CreateContact(account.Email, firstName, lastName, account.Address);
                if (newContactId == Guid.Empty)
                {
                    log.Error($"New contact with was NOT created!");
                }
                else
                {
                    log.Info($"New contact with Id: {newContactId} was created!");
                }
            }
        }

        private static void SplitName(string changedName, out string firstName, out string lastName)
        {
            string[] tokens = changedName.Split(' ')
                            .Where(x => x.Length > 0)
                            .ToArray();
            lastName = tokens.Last();

            if (tokens.Length >= 2)
            {
                firstName = string.Join(" ", tokens.Take(tokens.Length - 1));
            }
            else
            {
                firstName = null;
            }
        }
    }
}
