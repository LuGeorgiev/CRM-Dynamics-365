using System.Web.Mvc;

using StudentManagment.Models.Home;

namespace StudentManagment.Web.Controllers
{
    public class HomeController 
        : Controller
    {               

        public ActionResult Index()
        {
            var filter = new StudentFilter();

            return View(filter);
        }      
    }
}