using StudentManagment.Models.Case;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentManagment.Services
{
    public interface ICaseService
    {
        StudentWithCasesViewModel GetStudentCases(string studentId);

        bool ResolveCase(string ticketId);
    }
}
