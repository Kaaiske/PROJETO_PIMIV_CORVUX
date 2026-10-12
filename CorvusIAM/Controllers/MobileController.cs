using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorvusIAM.Controllers;

[Authorize]
public class MobileController : Controller
{
    public IActionResult Index() => View();
}
