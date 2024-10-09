using Microsoft.AspNetCore.Mvc;

namespace Proj_Frame.Controllers
{
    public class SubjectViewController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
