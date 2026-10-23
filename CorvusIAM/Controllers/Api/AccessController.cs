using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CorvusIAM.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Consulta de Permissões e Privilégios Efetivos do Colaborador Autenticado.
    /// </summary>
    [Authorize(Policy = "RequireJwt")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/[controller]")]
    [Tags("Permissões Efetivas do Colaborador")]
    public class AccessController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccessController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retorna a lista consolidada de permissões e privilégios concedidos ao colaborador autenticado através de seus perfis (RBAC).
        /// </summary>
        /// <response code="200">Lista de permissões do usuário retornada com sucesso.</response>
        /// <response code="401">Usuário não autenticado na sessão.</response>
        /// <response code="404">Colaborador não localizado na base de dados.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetAcessos()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var usuario = _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .ThenInclude(p => p.PerfilPermissoes)
                .ThenInclude(pp => pp.Permissao)
                .FirstOrDefault(u => u.Id == userId);

            if (usuario == null) return NotFound();

            var permissoes = usuario.UsuarioPerfis
                .SelectMany(up => up.Perfil.PerfilPermissoes)
                .Select(pp => new { name = pp.Permissao.Nome, description = pp.Permissao.Descricao })
                .Distinct()
                .ToList();

            return Ok(permissoes);
        }
    }
}
