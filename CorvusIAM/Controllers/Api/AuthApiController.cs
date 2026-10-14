using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Endpoint alternativo simplificado para integração direta de login via JSON.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Tags("Autenticação & Sessão (JWT / OAuth2)")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthApiController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// DTO para login simplificado.
        /// </summary>
        public class LoginDto
        {
            /// <summary>
            /// E-mail corporativo do usuário.
            /// </summary>
            public string Email { get; set; } = string.Empty;

            /// <summary>
            /// Senha do usuário.
            /// </summary>
            public string Senha { get; set; } = string.Empty;
        }

        /// <summary>
        /// Autenticação com payload simplificado e retorno direto do objeto de token.
        /// </summary>
        /// <param name="loginDto">E-mail e senha.</param>
        /// <response code="200">Autenticado com sucesso. Retorna token de sessão.</response>
        /// <response code="401">E-mail ou senha incorretos.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            var token = _authService.Autenticar(loginDto.Email, loginDto.Senha);
            if (token == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            return Ok(new { token });
        }
    }
}
