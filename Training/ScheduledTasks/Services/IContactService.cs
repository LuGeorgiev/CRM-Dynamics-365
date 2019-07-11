using Models.Contact;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface IContactService
    {
        IEnumerable<ContactRetrieveModel> AllContactsWithRegistrationInAccount(string registrationStatus , string registeredSubStatus);
    }
}
