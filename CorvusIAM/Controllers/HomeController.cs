using Microsoft.AspNetCore.Mvc;

namespace CorvusIAM.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
                if (User.IsInRole("Recursos Humanos")) return RedirectToAction("Usuarios", "Admin");
                if (User.IsInRole("Contabilidade")) return RedirectToAction("Contabilidade", "Portal");
                if (User.IsInRole("Help Desk")) return RedirectToAction("Chamados", "Admin");
                return RedirectToAction("MinhaConta", "Portal");
            }
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml", new CorvusIAM.Models.ErrorViewModel());
        }
    }
}
