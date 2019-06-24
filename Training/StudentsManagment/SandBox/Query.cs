using StudentManagment.Models.Student;
using System;
using System.Collections.Generic;
using System.Linq;

using FetchXml;
using StudentManagment.Models.Case;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace SandBox
{
    class Query
    {
        private readonly CrmCon db;

        public Query()
        {
            this.db = new CrmCon();
        }

        //Finished
        public IEnumerable<StudentViewModel> GetAllByStatus(int status)
        {
            var service = this.db.Connect();
            IList<StudentViewModel> result = null;

            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {
                    result = (from student in context.AccountSet
                              where student.StateCode.Equals(0)
                              select new StudentViewModel
                              {
                                  FirstName = student.New_FirstName,
                                  LastName = student.New_FamilyName,
                                  StudentId = student.New_UserID
                              })
                              .Take(30)
                              .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception throw during retrival with message: {ex.Message}");
            }


            return result;
        }
        //Finished
        public StudentDetailsViewModel GetStudentDetails(string studentId)
        {
            var service = this.db.Connect();
            StudentDetailsViewModel result = null;
            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {
                    result = (from student in context.AccountSet
                              where student.New_UserID.Equals(studentId)
                              select new StudentDetailsViewModel
                              {
                                  FirstName = student.New_FirstName,
                                  LastName = student.New_FamilyName,
                                  StudentId = student.New_UserID,
                                  StudentStatusValue = student.New_StudentStatus.Value,
                                  Program = student.new_programid.Name.ToString(),
                                  //ProgramAdvisor = student.new_ProgramAdvisorid.Name
                              })
                             .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception throw during retrival of students details with message: {ex.Message}");
            }
            return result;
        }
        //Test Putposes
        public void GetStudentDetailsTWO(string studentId)
        {
            var service = this.db.Connect();
            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {
                    var result = (from student in context.AccountSet
                                  where student.New_UserID.Equals(studentId)
                                  select student
                                ).FirstOrDefault();
                    ;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception throw during retrival of students details with message: {ex.Message}");
            }

        }
        //Finished
        public StudentWithCasesViewModel GetStudentCases(string studentId)
        {
            var service = this.db.Connect();
            StudentWithCasesViewModel result = null;
            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {

                    result = (from student in context.AccountSet
                              where student.New_UserID.Equals(studentId)
                              select new StudentWithCasesViewModel
                              {
                                  FirstName = student.New_FirstName,
                                  LastName = student.New_FamilyName,
                                  StudentId = student.New_UserID,
                                  Cases = (from stu in context.AccountSet
                                           join incident in context.IncidentSet on student.AccountId equals incident.CustomerId.Id
                                           where stu.New_UserID.Equals(studentId)
                                           select new CaseViewModel
                                           {
                                               Status = incident.new_caseid.Name,
                                               Subject = incident.SubjectId.Name,
                                               Title = incident.Title,
                                               TicketNumber = incident.TicketNumber
                                           })
                                           .ToList()
                              }
                              ).FirstOrDefault();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception throw during retrival of students cases with message: {ex.Message}");
            }

            return result;
        }

        //Under Dev
        public bool ResolveIncident(string ticketId)
        {
            var service = this.db.Connect();
            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {

                    var caseToResolve = (from incident in context.IncidentSet
                                  //join caseStatus in context.New_requeststatusSet on incident.new_caseid.Id equals caseStatus.New_requeststatusId
                                  where incident.TicketNumber.Equals(ticketId)
                                  select incident
                              )
                              .FirstOrDefault();

                    var statusesInCrm = (from status in context.New_requeststatusSet
                                         select status)
                                         .ToList();

                    //Change ticket CaseStatus to Completed
                    caseToResolve.new_caseid.Id = statusesInCrm[0].Id;

                    context.UpdateObject(caseToResolve);
                    context.SaveChanges();


                    var incidentResolution = new IncidentResolution
                    {
                        Subject = "Case Resolved",
                        IncidentId = new EntityReference(Incident.EntityLogicalName, caseToResolve.Id),
                        ActualEnd = DateTime.Now,

                    };

                    var closeIncidenRequst = new CloseIncidentRequest
                    {
                        IncidentResolution = incidentResolution,
                        Status = new OptionSetValue(5) //new OptionSetValue((int))
                    };

                   var response = (CloseIncidentResponse)service.Execute(closeIncidenRequst);                 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception throw during changing status of Ticket: {ex.Message}");
                return false;
            }


            return true;
        }
    }
}
