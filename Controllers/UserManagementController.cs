using Microsoft.AspNetCore.Mvc;

namespace Proj_Frame.Controllers
{
    public class UserManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
