using Microsoft.AspNetCore.Mvc;

namespace CleanSolution.Web.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ServiceController : Controller
    {
        public IActionResult Create()
        {
            return View();
        }
    }
}
