using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppSchedule.Tasks
{
    public interface IRegistrationTask
    {
        bool? Execute();

        //Only for Debugging delete when finish
        void TestMethod();
    }
}
