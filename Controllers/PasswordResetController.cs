using Microsoft.AspNetCore.Mvc;

namespace Proj_Frame.Controllers
{
    public class PasswordResetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
