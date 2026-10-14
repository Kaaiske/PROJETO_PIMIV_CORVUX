using Microsoft.AspNetCore.Mvc;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Gerenciamento de Autenticação Corporativa, Sessões e Emissão de Tokens JWT.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Autenticação & Sessão (JWT / OAuth2)")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Modelo de credenciais para autenticação de usuário.
        /// </summary>
        public class LoginRequest
        {
            /// <summary>
            /// Endereço de e-mail corporativo cadastrado (ex: admin@empresa.com).
            /// </summary>
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "Formato de email inválido.")]
            public string username { get; set; } = string.Empty;

            /// <summary>
            /// Senha de acesso do colaborador.
            /// </summary>
            [Required(ErrorMessage = "A senha é obrigatória.")]
            public string password { get; set; } = string.Empty;
        }

        /// <summary>
        /// Modelo para renovação de sessão através de refresh token.
        /// </summary>
        public class RefreshRequest
        {
            /// <summary>
            /// E-mail corporativo do titular da sessão.
            /// </summary>
            [Required] public string email { get; set; } = string.Empty;

            /// <summary>
            /// Refresh token emitido durante a autenticação inicial.
            /// </summary>
            [Required] public string refreshToken { get; set; } = string.Empty;
        }

        /// <summary>
        /// Autentica o colaborador corporativo e emite um novo par de tokens (JWT Bearer e Refresh Token).
        /// </summary>
        /// <remarks>
        /// Este endpoint conta com proteção ativa contra ataques de força bruta através de política de Rate Limiting (5 requisições por minuto por IP).
        /// O token JWT retornado deve ser enviado nos cabeçalhos subsequentes como:
        /// `Authorization: Bearer {token}`.
        /// </remarks>
        /// <param name="request">Credenciais de acesso (e-mail corporativo e senha).</param>
        /// <response code="200">Autenticação validada com sucesso. Retorna tokens de acesso.</response>
        /// <response code="400">Parâmetros de login inválidos ou não informados.</response>
        /// <response code="401">Credenciais inválidas, senha incorreta ou colaborador desativado no diretório.</response>
        /// <response code="429">Limite de tentativas de autenticação excedido. Aguarde 60 segundos.</response>
        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

        /// <summary>
        /// Renova um token de acesso JWT expirado a partir de um Refresh Token corporativo válido.
        /// </summary>
        /// <param name="request">E-mail corporativo e refresh token associado à sessão.</param>
        /// <response code="200">Sessão renovada com sucesso. Retorna novo token JWT.</response>
        /// <response code="400">Dados da requisição inválidos.</response>
        /// <response code="401">Refresh token expirado, inválido ou não pertencente ao usuário informado.</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
