using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CorvusIAM.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccessController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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
