using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

using ScheduldTaskTraining.Models.Account;
using ScheduldTaskTraining.Connection;

using XRM;
using log4net;


using static ScheduldTaskTraining.Constants;

namespace ScheduldTaskTraining.Services.Implementation
{
    public class AccountService : IAccountService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AccountService));

        private readonly IOrganizationService service;

        public AccountService(ICrmConnection connection)
        {
            this.service = connection.Connect();
        }

        //Retriev all accounts that has specific word in name attribute
        public IEnumerable<RetrievedAccountModel> AllContainingInName(string wordInName)
        {
            log.Info($"AllContainingInName started. Input param word in name: {wordInName}");            

            try
            {
                using (XrmServiceContext context = new XrmServiceContext(this.service))
                {
                    var result = context.AccountSet
                        .Where(x => x.StateCode == AccountState.Active && x.Name.Contains(wordInName))
                        .Select(x => new RetrievedAccountModel
                        {
                            Id = x.Id,
                            FullName = x.Name,
                            Email = x.EMailAddress1,
                            Address = x.Address1_Composite
                        })
                        .ToList();
                    log.Info($"Number of accounts retrieved: {result.Count}");

                    if (result != null && result.Count > 0)
                    {     
                        return result;
                    }
                    else
                    {
                        log.Info("Returned Collection was empty");
                        Console.WriteLine("Returned Collection was empty");
                        return Enumerable.Empty<RetrievedAccountModel>();
                    }
                }                
            }
            catch (Exception ex)
            {
                log.Error($"Exception throw during retrival with message: {ex.Message}");
                Console.WriteLine($"Exception throw during retrival with message: {ex.Message}");
                return Enumerable.Empty<RetrievedAccountModel>();
            }
        }

        //Edit name property by replacing string with another string
        public string ReplaceInName(AccountWithNameModel account, string wordToReplace, string replaceWith)
        {
            log.Info($"Account id: {account.Id} name: {account.FullName} will be edited");
            try
            {
                using (XrmServiceContext context = new XrmServiceContext(this.service))
                {
                    var newName = Regex.Replace(account.FullName, TO_REPLACE, REPLACE_WITH,RegexOptions.IgnoreCase);
                    var accountToChange = new Account
                    {
                        Id = account.Id,
                        Name = newName
                    };

                    this.service.Update(accountToChange);
                    log.Info($"AccountId: {accountToChange.Id} new name is: {newName}");

                    return newName;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception throw during name chaneg: {ex.Message}");
                Console.WriteLine($"Exception throw during name chaneg: {ex.Message}");
                return null;
            };
        }
    }
}
