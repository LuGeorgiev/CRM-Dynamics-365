using System.Collections.Generic;

namespace StudentManagment.Models.Case
{
    public class StudentWithCasesViewModel
    {
        public string StudentId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public IEnumerable<CaseViewModel> Cases { get; set; } = new HashSet<CaseViewModel>();
    }
}
