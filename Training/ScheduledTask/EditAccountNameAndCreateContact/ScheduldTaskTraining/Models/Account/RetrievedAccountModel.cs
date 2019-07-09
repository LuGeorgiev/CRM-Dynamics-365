using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduldTaskTraining.Models.Account
{
    public class RetrievedAccountModel 
        : AccountWithNameModel
    {   
        public string Email { get; set; }

        public string  Address { get; set; }
    }
}
