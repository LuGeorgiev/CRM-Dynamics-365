using ScheduldTaskTraining.Models.Account;
using System.Collections.Generic;


namespace ScheduldTaskTraining.Services
{
    public interface IAccountService
    {
        IEnumerable<RetrievedAccountModel> AllContainingInName(string name = "test");

        string ReplaceInName(AccountWithNameModel account, string wordToReplace, string replaceWith);
    }
}
