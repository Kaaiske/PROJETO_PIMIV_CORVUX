using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorvusIAM.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthApiController(IAuthService authService)
        {
            _authService = authService;
        }

        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Senha { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            var token = _authService.Autenticar(loginDto.Email, loginDto.Senha);
            if (token == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            return Ok(new { token });
        }
    }
}
