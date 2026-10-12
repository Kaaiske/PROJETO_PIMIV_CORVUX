using Microsoft.AspNetCore.Mvc;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public class LoginRequest
        {
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "Formato de email inválido.")]
            public string username { get; set; } = string.Empty;

            [Required(ErrorMessage = "A senha é obrigatória.")]
            public string password { get; set; } = string.Empty;
        }

        public class RefreshRequest
        {
            [Required] public string email { get; set; } = string.Empty;
            [Required] public string refreshToken { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = _authService.Autenticar(request.username, request.password);

            if (response == null)
            {
                return Unauthorized(new { mensagem = "Credenciais inválidas ou usuário inativo." });
            }

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken([FromBody] RefreshRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = _authService.AtualizarToken(request.email, request.refreshToken);

            if (response == null)
            {
                return Unauthorized(new { mensagem = "Token de renovação inválido ou expirado." });
            }

            return Ok(response);
        }
    }
}

