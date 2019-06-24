using System;
using System.Linq;

using StudentManagment.Data;
using StudentManagment.Logger;
using StudentManagment.Models.Case;
using FetchXml;

using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;

namespace StudentManagment.Services.Implementation
{
    public class CaseService : ICaseService
    {
        private const string SERVICE = "CaseService";
        private const string GET_STUDENT_CASES = "GetStudentCases method";
        private const string RESOLVE_CASES = "ResolveCasesr method";

        private readonly IOrganizationService service;

        public CaseService(IDbConnection db)
        {
            this.service = db.Connect();
        }

        public StudentWithCasesViewModel GetStudentCases(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
            {
                Log.Error($"{SERVICE} {GET_STUDENT_CASES}. Student Id was null or empty soace.");
            }
            Log.Info($"{SERVICE} {GET_STUDENT_CASES}. To retrieve casese for student id: {studentId}");
            
            //var service = this.db.Connect();            
            try
            {
                if (string.IsNullOrEmpty(studentId))
                {
                    Log.Error($"{SERVICE} {GET_STUDENT_CASES}. StudentId is null or empty");
                    return null;
                }
                StudentWithCasesViewModel studentWithCases = null;
                using (ServiceContext context = new ServiceContext(service))
                {
                    Log.Info($"{SERVICE} {GET_STUDENT_CASES}. Initiate request to CRM for studentId: {studentId}");

                    studentWithCases = (from student in context.AccountSet
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
                                                     .OrderBy(x => x.Status)
                                        }
                              ).FirstOrDefault();

                    if (studentWithCases == null)
                    {
                        Log.Error($"{SERVICE} {GET_STUDENT_CASES}. Student with StudentId: {studentId} was not found");
                        return null;
                    }

                    Log.Info($"{SERVICE} {GET_STUDENT_CASES}. Cases of student: {studentWithCases.StudentId} retrieved. Cases count: {studentWithCases.Cases.Count()}");

                    return studentWithCases;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{SERVICE} {GET_STUDENT_CASES}. Exception thrown studentId {studentId} request", ex);

                return null;
            }
        }

        public bool ResolveCase(string ticketId)
        {
            if (string.IsNullOrEmpty(ticketId))
            {
                Log.Error($"{SERVICE} {RESOLVE_CASES}. Ticket Id was find null or empty space.");
            }
            Log.Info($"{SERVICE} {RESOLVE_CASES}. To resolve ticket id: {ticketId}");

            //var service = this.db.Connect();
            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {
                    Log.Info($"{SERVICE} {GET_STUDENT_CASES}. Request for ticket {ticketId}");

                    var caseToResolve = (from incident in context.IncidentSet
                                         where incident.TicketNumber.Equals(ticketId)
                                         select incident
                              )
                              .FirstOrDefault();
                    if (caseToResolve == null)
                    {
                        Log.Error($"{SERVICE} {GET_STUDENT_CASES}. Ticket: {ticketId} was not found.");
                        return false;
                    }

                    //Check if the Case is already CLOSED/RESOLVED
                    if (caseToResolve.new_caseid.Name == "Closed" || caseToResolve.new_caseid.Name == "Completed")
                    {
                        Log.Error($"{SERVICE} {GET_STUDENT_CASES}. Ticket: {ticketId} status is not Active and cannot be Resolved.");
                        return false;
                    }
                    ;

                    //Retrieve the three type of statuses
                    var statusesInCrm = (from status in context.New_requeststatusSet
                                         select status)
                                         .ToList();

                    //Change ticket CaseStatus to Completed
                    caseToResolve.new_caseid.Id = statusesInCrm[0].Id;

                    Log.Info($"{SERVICE} {GET_STUDENT_CASES}. To Set ticket {ticketId} as completed");

                    context.UpdateObject(caseToResolve);
                    context.SaveChanges();


                    var incidentResolution = new IncidentResolution
                    {
                        Subject = "Case Resolved",
                        IncidentId = new EntityReference(Incident.EntityLogicalName, caseToResolve.Id),
                        ActualEnd = DateTime.Now
                    };
                    var closeIncidenRequst = new CloseIncidentRequest
                    {
                        IncidentResolution = incidentResolution,
                        Status = new OptionSetValue(5)
                    };

                    Log.Info($"{SERVICE} {GET_STUDENT_CASES}. To resolve ticket {ticketId}");

                    service.Execute(closeIncidenRequst);
                }

                Log.Info($"{SERVICE} {GET_STUDENT_CASES}. Ticket: {ticketId} was resolved");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception throw during resolving ticket wit id: {ticketId}", ex);
                return false;
            }

        }
    }
}
