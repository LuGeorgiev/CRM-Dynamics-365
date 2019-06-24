using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = new Query();

            //Test for Students by status
            //var collection = query.GetAllByStatus(0);

            //Get Student Details tests
            //var student = query.GetStudentDetails("S101199");
            //query.GetStudentDetailsTWO("S110895");

            //Test for Casese query
            // var collection = query.GetStudentCases("S101199");

            var ticket = query.ResolveIncident("CAS-04525-P8R6H4");
            Console.WriteLine(ticket);
            ;

        }
    }
}
