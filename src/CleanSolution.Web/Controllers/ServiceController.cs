using Microsoft.AspNetCore.Mvc;

namespace CleanSolution.Web.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Details()
        {
            return View();
        }
        public IActionResult ProviderDetails()
        {
            return View();
        }
    }
}
