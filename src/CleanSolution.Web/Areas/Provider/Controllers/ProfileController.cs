using Microsoft.AspNetCore.Mvc;

namespace CleanSolution.Web.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
