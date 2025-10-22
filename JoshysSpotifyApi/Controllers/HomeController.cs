using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers 
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.StatusMessage = TempData["ResponseJson"];
            ViewBag.StatusMessage = TempData["Access_Token"];
            ViewBag.StatusMessage = TempData["Refresh_Token"];
            return View();
        }
    }
}
