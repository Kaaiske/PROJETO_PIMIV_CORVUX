using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorvusIAM.Controllers;

[Route("Mobile")]
public class MobileController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    [Authorize]
    public IActionResult Index() => View();

    /// <summary>
    /// Ponto de entrada para autenticação em clientes móveis e PWA.
    /// Redireciona com segurança para a tela oficial de login corporativo preservando o retorno para a interface mobile.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        var safeReturnUrl = (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            ? returnUrl
            : "/Mobile";
        return Redirect($"/Auth/Login?returnUrl={System.Uri.EscapeDataString(safeReturnUrl)}");
    }
}
