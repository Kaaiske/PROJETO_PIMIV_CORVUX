using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorvusIAM.Controllers;

[Authorize]
public class MobileController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Ponto de entrada para autenticação em clientes móveis e PWA.
    /// Redireciona com segurança para a tela oficial de login corporativo preservando o retorno para a interface mobile.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("Mobile/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction("Login", "Auth", new { returnUrl = returnUrl ?? "/Mobile/Index" });
    }
}
