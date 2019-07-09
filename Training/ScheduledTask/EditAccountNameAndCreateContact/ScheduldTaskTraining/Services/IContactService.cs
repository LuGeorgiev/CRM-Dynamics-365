using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduldTaskTraining.Services
{
    public interface IContactService
    {
        Guid CreateContact(string email, string firstName, string lastName, string address);
    }
}
