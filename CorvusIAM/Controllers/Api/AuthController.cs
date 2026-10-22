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
        /// Modelo para validação de segundo fator (MFA / TOTP) via API.
        /// </summary>
        public class MfaVerifyRequest
        {
            /// <summary>
            /// Token de sessão MFA temporário emitido na chamada de login.
            /// </summary>
            [Required(ErrorMessage = "O token de sessão MFA é obrigatório.")]
            public string mfaSessionToken { get; set; } = string.Empty;

            /// <summary>
            /// Código numérico de 6 dígitos gerado pelo autenticador TOTP.
            /// </summary>
            [Required(ErrorMessage = "O código TOTP é obrigatório.")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve conter exatamente 6 dígitos.")]
            public string codigoTotp { get; set; } = string.Empty;
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
        /// Redireciona requisições GET feitas via navegador para a interface de Login Web amigável.
        /// </summary>
        [HttpGet("login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult LoginGet()
        {
            return Redirect("/Auth/Login");
        }

        /// <summary>
        /// Autentica o colaborador corporativo e emite um novo par de tokens (JWT Bearer e Refresh Token).
        /// Se o colaborador possuir MFA ativo, retorna mfaRequerido=true e um mfaSessionToken temporário para validação no endpoint /mfa-verify.
        /// </summary>
        /// <param name="request">Credenciais de acesso (e-mail corporativo e senha).</param>
        /// <response code="200">Autenticação validada com sucesso. Retorna tokens de acesso ou requisição de MFA.</response>
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
        /// Valida o segundo fator (TOTP) para contas com MFA ativo e emite os tokens finais de acesso.
        /// </summary>
        /// <param name="request">Token de sessão MFA temporário e código numérico de 6 dígitos.</param>
        /// <response code="200">Segundo fator validado. Retorna tokens JWT Bearer e Refresh Token.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="401">Código incorreto ou sessão MFA expirada/bloqueada por tentativas.</response>
        [HttpPost("mfa-verify")]
        [EnableRateLimiting("LoginPolicy")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult MfaVerify([FromBody] MfaVerifyRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = _authService.AutenticarMfa(request.mfaSessionToken, request.codigoTotp);
            if (response == null)
            {
                return Unauthorized(new { mensagem = "Código de autenticação incorreto ou sessão expirada. Tente novamente." });
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
