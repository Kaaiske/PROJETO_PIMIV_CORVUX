using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorvusIAM.Data;
using CorvusIAM.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Controllers {
    [Authorize]
    public class PortalController : Controller {
        
        private readonly ApplicationDbContext _context;
        private readonly IPasswordPwnedService _passwordPwned;

        public PortalController(ApplicationDbContext context, IPasswordPwnedService passwordPwned)
        {
            _context = context;
            _passwordPwned = passwordPwned;
        }

        [Authorize(Policy = "RequireHR")]
        public IActionResult RH() { ViewData["Role"] = "RH"; ViewData["UserName"] = User.Identity?.Name ?? "Usuário"; return View(); }
        
        [Authorize(Policy = "RequireAccounting")]
        public IActionResult Contabilidade() { ViewData["Role"] = "Contabilidade"; ViewData["UserName"] = User.Identity?.Name ?? "Usuário"; return View(); }
        
        [Authorize(Policy = "RequireHelpDesk")]
        public IActionResult HelpDesk() { ViewData["Role"] = "Suporte T.I."; ViewData["UserName"] = User.Identity?.Name ?? "Usuário"; return View(); }
        
        public IActionResult MinhaConta() {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login", "Auth");
            int userId = int.Parse(userIdClaim);

            var user = _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .FirstOrDefault(u => u.Id == userId);
                
            if (user == null) return RedirectToAction("Login", "Auth");

            var chamados = _context.ChamadosSuporte
                .Where(c => c.UsuarioId == userId)
                .OrderByDescending(c => c.DataAbertura)
                .Take(5)
                .ToList();

            var logs = _context.LogsAuditoria
                .Where(l => l.UsuarioId == userId)
                .OrderByDescending(l => l.DataHora)
                .Take(5)
                .ToList();

            ViewData["Role"] = user.UsuarioPerfis.FirstOrDefault()?.Perfil?.Nome ?? "Colaborador"; 
            ViewData["UserName"] = user.Nome;

            var model = new MinhaContaViewModel {
                Usuario = user,
                Chamados = chamados,
                Logs = logs
            };

            return View(model); 
        }

        [HttpPost]
        public IActionResult AbrirChamado([FromBody] NovoChamadoDTO dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var chamado = new CorvusIAM.Entities.ChamadoSuporte {
                UsuarioId = int.Parse(userIdClaim),
                Assunto = dto.Assunto,
                Descricao = dto.Descricao,
                Status = "Aberto",
                DataAbertura = System.DateTime.Now
            };

            _context.ChamadosSuporte.Add(chamado);
            _context.SaveChanges();

            return Ok(new { message = "Chamado aberto com sucesso!" });
        }

        public class MinhaContaViewModel {
            public CorvusIAM.Entities.Usuario Usuario { get; set; } = null!;
            public System.Collections.Generic.List<CorvusIAM.Entities.ChamadoSuporte> Chamados { get; set; } = new();
            public System.Collections.Generic.List<CorvusIAM.Entities.LogAuditoria> Logs { get; set; } = new();
        }

        public class NovoChamadoDTO {
            public string Assunto { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDTO dto)
        {
            if (dto.NovaSenha != dto.ConfirmaSenha) return BadRequest(new { message = "As senhas não coincidem." });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int userId = int.Parse(userIdClaim);
            var user = _context.Usuarios.Find(userId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, user.SenhaHash))
            {
                return BadRequest(new { message = "A senha atual está incorreta." });
            }

            if (dto.NovaSenha.Length < 8 || !dto.NovaSenha.Any(char.IsUpper) || !dto.NovaSenha.Any(char.IsLower) || !dto.NovaSenha.Any(char.IsDigit) || !dto.NovaSenha.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                return BadRequest(new { message = "A senha deve conter no mínimo 8 caracteres, maiúsculas, minúsculas, números e símbolos especiais." });
            }

            // Validar com a API Have I Been Pwned
            bool isPwned = await _passwordPwned.IsPasswordPwnedAsync(dto.NovaSenha);
            if (isPwned)
            {
                return BadRequest(new { message = "Esta senha já vazou na internet (Have I Been Pwned). Por motivos de segurança, escolha uma senha mais forte." });
            }

            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            
            _context.LogsAuditoria.Add(new CorvusIAM.Entities.LogAuditoria { 
                DataHora = System.DateTime.Now, 
                Acao = "Alteração de Senha", 
                Detalhe = $"O usuário {user.Email} alterou a própria senha.", 
                Tipo = "Info", 
                Origem = "Portal Colaborador", 
                UsuarioId = userId 
            });

            _context.SaveChanges();

            return Ok(new { message = "Senha alterada com sucesso!" });
        }

        public class AlterarSenhaDTO
        {
            public string SenhaAtual { get; set; } = string.Empty;
            public string NovaSenha { get; set; } = string.Empty;
            public string ConfirmaSenha { get; set; } = string.Empty;
        }
    }
}

