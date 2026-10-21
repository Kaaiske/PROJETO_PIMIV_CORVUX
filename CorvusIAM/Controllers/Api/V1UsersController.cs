using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Data;
using CorvusIAM.Entities;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Gateway de Integração Inbound (v1) para sincronização e consulta de colaboradores por sistemas externos (ERPs, CRMs, Webhooks).
    /// </summary>
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/v1")]
    [Tags("Diretório & Colaboradores (v1 Inbound Gateway)")]
    public class V1UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public V1UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthorized()
        {
            if (User?.Identity?.IsAuthenticated == true) return true;

            string? token = null;
            if (Request.Headers.TryGetValue("X-API-Key", out var headerKey))
            {
                token = headerKey.ToString();
            }
            else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var val = authHeader.ToString();
                if (val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = val.Substring("Bearer ".Length).Trim();
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                var expected = _context.ConfiguracoesSistema.FirstOrDefault(c => c.Chave == "API_GATEWAY_TOKEN")?.Valor;
                if (!string.IsNullOrEmpty(expected))
                {
                    var tokenBytes = Encoding.UTF8.GetBytes(token);
                    var expectedBytes = Encoding.UTF8.GetBytes(expected);
                    if (CryptographicOperations.FixedTimeEquals(tokenBytes, expectedBytes))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Healthcheck e verificação de disponibilidade do Gateway Inbound v1.
        /// </summary>
        /// <response code="200">Serviço online e operando em conformidade.</response>
        [HttpGet("ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Ping()
        {
            return Ok(new
            {
                status = "ONLINE",
                service = "CorvuxIAM Inbound Gateway",
                version = "v1.0",
                timestamp = DateTime.UtcNow,
                domain = "https://corvuxiam.tech"
            });
        }

        /// <summary>
        /// Consulta paginada dos colaboradores ativos no diretório corporativo.
        /// </summary>
        /// <remarks>
        /// Requer autenticação via cabeçalho HTTP:
        /// - `X-API-Key: {token_do_gateway}` ou
        /// - `Authorization: Bearer {token_jwt}`.
        /// </remarks>
        /// <param name="page">Número da página atual (inicia em 1).</param>
        /// <param name="pageSize">Quantidade máxima de registros por página (padrão: 50).</param>
        /// <response code="200">Página de colaboradores retornada com sucesso.</response>
        /// <response code="401">Chave de API inválida, revogada ou ausente no cabeçalho.</response>
        [HttpGet("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { error = "Chave de API inválida ou ausente. Forneça o token via header 'X-API-Key' ou 'Authorization: Bearer {token}'." });
            }

            if (pageSize > 200) pageSize = 200;
            if (page < 1) page = 1;

            var query = _context.Usuarios.AsNoTracking().Where(u => u.Ativo);
            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Nome)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Nome,
                    u.Email,
                    u.Cargo,
                    u.Departamento,
                    u.Ativo,
                    u.MfaHabilitado,
                    u.DataContratacao
                })
                .ToListAsync();

            return Ok(new
            {
                page,
                pageSize,
                total,
                users
            });
        }

        /// <summary>
        /// Obtém os dados detalhados e os perfis vinculados a um colaborador específico pelo identificador.
        /// </summary>
        /// <param name="id">Identificador numérico exclusivo do colaborador.</param>
        /// <response code="200">Colaborador localizado com sucesso.</response>
        /// <response code="401">Chave de API ausente ou inválida.</response>
        /// <response code="404">Colaborador não encontrado na base de dados.</response>
        [HttpGet("users/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(int id)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { error = "Chave de API inválida ou ausente." });
            }

            var user = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound(new { error = "Colaborador não encontrado." });

            return Ok(new
            {
                user.Id,
                user.Nome,
                user.Email,
                user.Cargo,
                user.Departamento,
                user.Ativo,
                user.MfaHabilitado,
                user.DataContratacao,
                Perfis = user.UsuarioPerfis.Select(p => p.Perfil.Nome).ToList()
            });
        }
    }
}
