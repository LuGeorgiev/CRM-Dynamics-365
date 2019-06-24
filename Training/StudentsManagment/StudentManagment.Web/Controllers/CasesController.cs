using System.Linq;
using System.Web.Mvc;

using StudentManagment.Logger;
using StudentManagment.Services;
using StudentManagment.Web.Utilities;

namespace StudentManagment.Web.Controllers
{
    public class CasesController : Controller
    {
        private const string CONTROLLER = "Cases";
        private const string INDEX_ACTION = "Index";
        private const string RESOLVE_ACTION = "Resolve";

        private readonly ICaseService caseService;

        public CasesController(ICaseService caseService)
        {            
            this.caseService = caseService;
        }


        public ActionResult Cases(string id)
        {
            Log.Info($"{CONTROLLER}{INDEX_ACTION}. Request for Cases of student with Id: {id}");

            if (string.IsNullOrEmpty(id))
            {
                Log.Error($"{CONTROLLER}{INDEX_ACTION}. model null or empty space value! ");
                return View("Error");
            }

            try
            {
                var model = caseService.GetStudentCases(id);

                if (model == null)
                {
                    Log.Warn($"{CONTROLLER}{INDEX_ACTION}. Null collection retrieved for student with Id: {id}");
                    TempData.AddErrorMessage($"Cases for student with Id: {id} were not Found!");

                    return this.RedirectToAction("Details", "Students", new { id = id });
                }
                else
                {
                    Log.Info($"{CONTROLLER}{INDEX_ACTION}. Collection successfully retrieved. Cases retrieved: {model.Cases.Count()}");
                    return View(model);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"{CONTROLLER}{INDEX_ACTION}. Exception occuerd!",ex);
                return View("Error");
            }


        }

        //TODO Refactor to be Http POST
        public ActionResult Resolve(string ticket, string id)
        {
            Log.Info($"{CONTROLLER}{RESOLVE_ACTION}. Case number: {ticket} for student with Id {id} is to be resolved.");
            if (string.IsNullOrEmpty(ticket) || string.IsNullOrEmpty(id))
            {
                Log.Error($"{CONTROLLER}{RESOLVE_ACTION}. model null or empty space value! ");
                return View("Error");
            }

            try
            {
                var wasResolved = caseService.ResolveCase(ticket);

                if (wasResolved)
                {
                    Log.Info($"{CONTROLLER}{RESOLVE_ACTION}.Case Id:{ticket} was resolved");
                    TempData.AddSuccessMessage($"Case with Id: {ticket} of student with Id: {id} was resolved");
                }
                else
                {
                    Log.Info($"{CONTROLLER}{RESOLVE_ACTION}.Case Id:{ticket} was NOT resolved");
                    TempData.AddErrorMessage($"Case with Id: {ticket} of student with Id: {id} was NOT resolved");
                }

                return this.RedirectToAction("Index", new { id });
            }
            catch (System.Exception ex)
            {

                Log.Error($"{CONTROLLER}{RESOLVE_ACTION}. Exception occuerd!", ex);
                return View("Error");
            }
        }
    }
}