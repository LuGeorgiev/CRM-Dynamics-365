using Models.Account;
using System.Collections.Generic;

namespace Models.Contact
{
    public class ContactRetrieveModel
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string LastName { get; set; }

        public AccountRetreiveModel Account { get; set; }
    }
}
