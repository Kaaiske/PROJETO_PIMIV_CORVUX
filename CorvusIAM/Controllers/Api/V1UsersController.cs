using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Data;
using CorvusIAM.Entities;

namespace CorvusIAM.Controllers.Api
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/v1")]
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
                if (!string.IsNullOrEmpty(expected) && token == expected)
                {
                    return true;
                }
            }

            return false;
        }

        [HttpGet("ping")]
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

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { error = "Chave de API inválida ou ausente. Forneça o token via header 'X-API-Key' ou 'Authorization: Bearer {token}'." });
            }

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

        [HttpGet("users/{id}")]
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

