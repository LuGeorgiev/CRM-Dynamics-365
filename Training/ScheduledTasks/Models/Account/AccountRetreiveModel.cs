using Models.Contact;
using Models.Registration;
using System.Collections.Generic;

namespace Models.Account
{
    public class AccountRetreiveModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public ICollection<RegistrationRetreiveModel> Registrations { get; set; } = new HashSet<RegistrationRetreiveModel>();
    }
}
