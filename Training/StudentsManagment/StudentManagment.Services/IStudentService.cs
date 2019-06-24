using StudentManagment.Models.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentManagment.Services
{
    public interface IStudentService
    {
        IEnumerable<StudentViewModel> GetAllByStatus(int status);

        StudentDetailsViewModel GetStudentDetails(string studentId);

        bool EditStudentDetails(StudentDetailsBindingModel student);
    }
}
