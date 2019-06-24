using System.Linq;
using System.Net;
using System.Web.Mvc;
using StudentManagment.Logger;
using StudentManagment.Models.Home;
using StudentManagment.Models.Student;
using StudentManagment.Services;
using StudentManagment.Web.Utilities;

using X.PagedList;

namespace StudentManagment.Web.Controllers
{
    public class StudentsController : Controller
    {
        private const string CONTROLLER = "Students";
        private const string INDEX_ACTION = "Index";
        private const string DETAILS_ACTION = "Details";
        private const string EDIT_ACTION = "Edit";

        private const int PAGE_SIZE = 10;
        private static int PreviousStatus;

        readonly IStudentService studentsService;

        public StudentsController(IStudentService students)
        {
            this.studentsService = students;
        }

        // GET: Students
        public ActionResult Index(int? page, StudentFilter input)
        {
            Log.Info($"{CONTROLLER}{INDEX_ACTION}. Page to show: {page??1} students with status:{input.Status}");
            if (!this.ModelState.IsValid)
            {
                Log.Error($"{CONTROLLER}{INDEX_ACTION}. Model is not valid status binded was: {input.Status}");
                TempData.AddErrorMessage("Please input valid status for students");

                return this.RedirectToAction("Index", "Home", input);
            }

            try
            {
                if (input.Status != -1)
                {
                    PreviousStatus = input.Status;
                }

                int queryStatus = input.Status == -1 
                    ? PreviousStatus 
                    : input.Status;


                var pageNumber = page ?? 1;
                var model = this.studentsService.GetAllByStatus(queryStatus);

                string filterStatus = FillStatus(PreviousStatus);

                if (model==null)
                {
                    Log.Info($"{CONTROLLER}{INDEX_ACTION}. No students with status: {filterStatus} were retrieved.");
                    TempData.AddErrorMessage($"Students with status: {filterStatus} were not found.");

                    return RedirectToAction("Index", "Home");
                }

                var onePageFromModel = model.ToPagedList(pageNumber, PAGE_SIZE);

                Log.Info($"{CONTROLLER}{INDEX_ACTION}. Student with status {filterStatus} were retrieved. Total count: {model.Count()}");
                TempData.AddSuccessMessage($"Filter for {filterStatus} students, page: {pageNumber}!");

                return View(onePageFromModel);

            }
            catch (System.Exception ex)
            {
                Log.Error($"{CONTROLLER}{INDEX_ACTION}. Exception thrown",ex);
                return View("Error");
            }   

        }


        public ActionResult Details(string id)
        {
            Log.Info($"{CONTROLLER}{DETAILS_ACTION}. Student id: {id}");
            try
            {
                var model = studentsService.GetStudentDetails(id);

                if (model == null)
                {
                    Log.Error($"{CONTROLLER}{DETAILS_ACTION}. Student id: {id} was not found. Bad request response!");
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Entyty do not exist");
                }

                Log.Info($"{CONTROLLER}{DETAILS_ACTION}. Student id: {id} was retrieved.");

                return View(model);

            }
            catch (System.Exception ex)
            {
                Log.Error($"{CONTROLLER}{DETAILS_ACTION}. Exception thrown.", ex);

                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult Edit(StudentDetailsBindingModel model)
        {
            Log.Info($"{CONTROLLER}{EDIT_ACTION}. Fields to be edited:Id: {model.StudentId}, First name: {model.FirstName}, Last name: {model.LastName}");

            try
            {
                if (!this.ModelState.IsValid)
                {
                    Log.Error($"{CONTROLLER}{EDIT_ACTION}. Invalid Model Id: {model.StudentId}, First name: {model.FirstName}, Last name: {model.LastName}");
                    TempData.AddErrorMessage("Please, fill in information correctly");

                    return RedirectToAction("Details", new { id = model.StudentId });
                }

                var isEdited = this.studentsService.EditStudentDetails(model);

                if (isEdited)
                {
                    Log.Info($"Sucessfully edited id {model.StudentId}");
                    TempData.AddSuccessMessage("Name/s information was successfully edited");
                }
                else
                {
                    Log.Error($"NOT Sucessfully edited id {model.StudentId}");
                    TempData.AddErrorMessage("Name/s information was not changed");
                }

                return RedirectToAction("Details", new { id = model.StudentId });

            }
            catch (System.Exception ex)
            {

                Log.Error($"{CONTROLLER}{EDIT_ACTION}. Exceprion thrown.", ex);

                return View("Error");
            }
        }


        private string FillStatus(int status)
        {
            if (status == 1)
            {
                return "Enrolled";
            }
            else if (status == 2)
            {
                return "Graduated";
            }
            else if (status == 3)
            {
                return "Never Marticulated";
            }
            else if (status == 4)
            {
                return "On Hold";
            }
            else if (status == 5)
            {
                return "Unenrolled";
            }
            else if (status == 0)
            {
                return "All";
            }
            else
            {
                return "NOT Correct status filter for";
            }
        }
    }
}