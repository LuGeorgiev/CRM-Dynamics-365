using System;
using System.Collections.Generic;
using System.Linq;
using StudentManagment.Data;
using StudentManagment.Models.Student;

using FetchXml;
using StudentManagment.Logger;
using Microsoft.Xrm.Sdk;

namespace StudentManagment.Services.Implementation
{
    public class StudentService : IStudentService
    {
        private const string SERVICE = "StudentService";
        private const string EDIT_DETAILS = "EditStudentDetails method";
        private const string GET_ALL_BY_STATUS = "GetAllByStatus method";
        private const string GET_DETAILS = "GetStudentDetails method";

        private readonly IOrganizationService service;

        public StudentService(IDbConnection db)
        {
            this.service = db.Connect();
        }

        public bool EditStudentDetails(StudentDetailsBindingModel student)
        {
            if (student == null)
            {
                Log.Error($"Inpput student model was null");
                return false;
            }
            Log.Info($"{SERVICE} {EDIT_DETAILS}. To edit studentId {student.StudentId}");

            //var service = this.db.Connect();

            try
            {
                using (ServiceContext context = new ServiceContext(service))
                {
                    Log.Info($"{SERVICE} {EDIT_DETAILS}. Request to CRM studentId {student.StudentId}");

                    var retrived = (from x in context.AccountSet
                                    where x.New_UserID.Equals(student.StudentId)
                                    select x)
                                   .FirstOrDefault();

                    if (retrived == null)
                    {
                        Log.Error($"{SERVICE} {EDIT_DETAILS}. StudentId {student.StudentId} was not found.");
                        return false;
                    }

                    bool attribuTeWasChanged = false;
                    if (retrived.New_FirstName != student.FirstName)
                    {
                        Log.Info($"{SERVICE} {EDIT_DETAILS}. StudentId {student.StudentId} first name will be changed to {student.FirstName}");
                        retrived.New_FirstName = student.FirstName;
                        attribuTeWasChanged = true;
                    }

                    if (retrived.New_FamilyName != student.LastName)
                    {
                        Log.Info($"{SERVICE} {EDIT_DETAILS}. StudentId {student.StudentId} last name will be changed to {student.LastName}");
                        retrived.New_FamilyName = student.LastName;
                        attribuTeWasChanged = true;
                    }

                    if (attribuTeWasChanged)
                    {
                        context.UpdateObject(retrived);
                        context.SaveChanges();

                        Log.Info($"{SERVICE} {EDIT_DETAILS}. StudentId {student.StudentId} changes were saved");
                    }
                    else
                    {
                        Log.Info($"{SERVICE} {EDIT_DETAILS}. StudentId {student.StudentId} has no fileds that are to be changed");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //TODO LOG
                Log.Error($"Error during Edit studentId {student.StudentId}", ex);
                return false;
            }

        }

        public IEnumerable<StudentViewModel> GetAllByStatus(int status)
        {
            Log.Info($"{SERVICE} {GET_ALL_BY_STATUS}. To Get all students with status {status}.");

            //var service = this.db.Connect();

            try
            {
                IList<StudentViewModel> result = null;
                using (ServiceContext context = new ServiceContext(service))
                {
                    Log.Info($"{SERVICE} {EDIT_DETAILS}. Request to CRM for students with status {status}");

                    if (status == 0)
                    {
                        result = (from student in context.AccountSet
                                  where student.StateCode.Equals(0)
                                  orderby student.New_UserID
                                  select new StudentViewModel
                                  {
                                      FirstName = student.New_FirstName,
                                      LastName = student.New_FamilyName,
                                      StudentId = student.New_UserID
                                  })
                                  .ToList();

                    }
                    else
                    {
                        result = (from student in context.AccountSet
                                  where student.StateCode.Equals(0)
                                     && student.New_StudentStatus.Value.Equals(status)
                                  orderby student.New_UserID
                                  select new StudentViewModel
                                  {
                                      FirstName = student.New_FirstName,
                                      LastName = student.New_FamilyName,
                                      StudentId = student.New_UserID
                                  })
                                  .ToList();
                    }

                }
                Log.Info($"{SERVICE} {EDIT_DETAILS}. Sdudents with status {status} retrieved: {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"{SERVICE} {EDIT_DETAILS}.Exception throw during retrival with message: {ex.Message}");
                return null;
            }
        }

        public StudentDetailsViewModel GetStudentDetails(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
            {
                Log.Error($"{SERVICE} {GET_DETAILS}. Studetn id was null");
                return null;
            }
            Log.Info($"{SERVICE} {GET_DETAILS}. To Get details of student: {studentId}.");

            //var service = this.db.Connect();
            try
            {
                StudentDetailsViewModel result = null;
                using (ServiceContext context = new ServiceContext(service))
                {
                    Log.Info($"To request from CRM details of student: {studentId}");

                    result = (from student in context.AccountSet
                              where student.New_UserID.Equals(studentId)
                              select new StudentDetailsViewModel
                              {
                                  FirstName = student.New_FirstName,
                                  LastName = student.New_FamilyName,
                                  StudentId = student.New_UserID,
                                  StudentStatusValue = student.New_StudentStatus.Value,
                                  Program = student.new_programid == null
                                        ? "Program not filled in"
                                        : student.new_programid.Name.ToString(),
                                  ProgramAdvisor = student.new_ProgramAdvisorid == null
                                        ? "Program advisor not filled in"
                                        : student.new_ProgramAdvisorid.Name.ToString()
                              })
                             .FirstOrDefault();
                }
                if (result == null)
                {
                    Log.Error($"{SERVICE} {GET_DETAILS}. Student was not found Id {studentId}");
                }
                else
                {
                    Log.Info($"{SERVICE} {GET_DETAILS}. Student {studentId} was retrieved.");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"{SERVICE} {GET_DETAILS}. Exception throw during retrival of students details {studentId}", ex);
                return null;
            }
        }
    }
}
