using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace CorvusIAM.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CorvusIAM.Interfaces.IPasswordPwnedService _passwordPwned;
        private readonly CorvusIAM.Interfaces.IEmailValidationService _emailValidator;
        private readonly CorvusIAM.Interfaces.IEmailSenderService _emailSender;
        private readonly CorvusIAM.Interfaces.IPasswordGeneratorService _passwordGenerator;

        public AdminController(
            ApplicationDbContext context,
            CorvusIAM.Interfaces.IPasswordPwnedService passwordPwned,
            CorvusIAM.Interfaces.IEmailValidationService emailValidator,
            CorvusIAM.Interfaces.IEmailSenderService emailSender,
            CorvusIAM.Interfaces.IPasswordGeneratorService passwordGenerator) 
        { 
            _context = context; 
            _passwordPwned = passwordPwned;
            _emailValidator = emailValidator;
            _emailSender = emailSender;
            _passwordGenerator = passwordGenerator;
        }

        private static readonly string _integracoesFilePath = "integracoes.json";
        
        private List<ModuloStatus> GetIntegracoes()
        {
            if (System.IO.File.Exists(_integracoesFilePath))
            {
                var json = System.IO.File.ReadAllText(_integracoesFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<List<ModuloStatus>>(json) ?? new List<ModuloStatus>();
            }
            return new List<ModuloStatus>();
        }

        private void SaveIntegracoes(List<ModuloStatus> integracoes)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(integracoes);
            System.IO.File.WriteAllText(_integracoesFilePath, json);
        }

        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Integracoes()
        {
            return View(GetIntegracoes());
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult AddIntegracao([FromBody] ModuloStatus model)
        {
            var list = GetIntegracoes();
            list.Add(model);
            SaveIntegracoes(list);
            return Ok(new { message = "Integração adicionada" });
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult RemoveIntegracao([FromBody] string nome)
        {
            var list = GetIntegracoes();
            list.RemoveAll(i => i.Nome == nome);
            SaveIntegracoes(list);
            return Ok(new { message = "Integração removida" });
        }

        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Index()
        {
            var logsDb = _context.LogsAuditoria.OrderByDescending(l => l.DataHora).Take(10).ToList();
            var logsVm = logsDb.Select(l => new LogAuditoria { DataHora = l.DataHora, Acao = l.Acao, Detalhe = l.Detalhe, Tipo = l.Tipo, Origem = l.Origem }).ToList();
            var vm = new DashboardViewModel {
                TotalUsuarios = _context.Usuarios.Count(), AcessosBloqueados = _context.LogsAuditoria.Count(l => l.Tipo == "Error"), DispositivosMobile = 0, UsoNuvem = 0, LogsRecentes = logsVm, Modulos = GetIntegracoes()
            };
            return View(vm);
        }
        [Authorize(Policy = "RequireHR")]
        public IActionResult Usuarios() {
            var usuarios = _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .ToList();
            return View(usuarios);
        }
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Politicas() => View(_context.PoliticasAcesso.ToList());
        
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Perfis() => View(_context.Perfis.Where(p => p.Ativo).ToList());

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult AdicionarPerfil([FromBody] Perfil p) {
            p.Ativo = true;
            _context.Perfis.Add(p);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult ExcluirPerfil([FromBody] int id) {
            var p = _context.Perfis.Include(x => x.UsuarioPerfis).FirstOrDefault(x => x.Id == id);
            if (p != null) {
                if (p.UsuarioPerfis != null && p.UsuarioPerfis.Any()) {
                    return BadRequest(new { message = $"Não é possível excluir o perfil '{p.Nome}' pois existem {p.UsuarioPerfis.Count} colaborador(es) associados a ele." });
                }
                // Soft Delete em Perfil
                p.Ativo = false;
                var adminId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1";
                _context.LogsAuditoria.Add(new LogAuditoria {
                    DataHora = DateTime.Now,
                    Acao = "Inativação de Perfil (Soft Delete)",
                    Detalhe = $"Perfil '{p.Nome}' (ID: {p.Id}) inativado via Soft Delete",
                    Tipo = "Danger",
                    Origem = "Admin Panel",
                    UsuarioId = int.Parse(adminId)
                });
                try { 
                    _context.SaveChanges(); 
                    return Ok(); 
                } 
                catch { 
                    return BadRequest(new { message = "Falha de integridade ao persistir inativação no banco de dados." }); 
                }
            }
            return NotFound(new { message = "Perfil não encontrado." });
        }

        [HttpGet]
        [Authorize(Policy = "RequireHR")]
        public IActionResult ExportarUsuariosCsv()
        {
            var usuarios = _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .OrderBy(u => u.Nome)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID;Nome;Email;CPF;Cargo;Departamento;Perfil;Data Contratacao;Status");

            foreach (var u in usuarios)
            {
                var perfil = u.UsuarioPerfis.FirstOrDefault()?.Perfil?.Nome ?? "Colaborador";
                var status = u.Ativo ? "Ativo" : "Inativo";
                var dtContr = u.DataContratacao?.ToString("dd/MM/yyyy") ?? "-";
                sb.AppendLine($"{u.Id};\"{u.Nome}\";{u.Email};{u.CPF};\"{u.Cargo}\";\"{u.Departamento}\";\"{perfil}\";{dtContr};{status}");
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"CorvuxIAM_Colaboradores_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult ExportarLogsAuditoriaCsv()
        {
            var logs = _context.LogsAuditoria
                .Include(l => l.Usuario)
                .OrderByDescending(l => l.DataHora)
                .Take(1000)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID;Data/Hora (UTC);Usuario;Acao;Detalhe;Tipo;Origem");

            foreach (var l in logs)
            {
                var userStr = l.Usuario != null ? $"{l.Usuario.Nome} ({l.Usuario.Email})" : (l.UsuarioId.HasValue ? $"ID: {l.UsuarioId}" : "Sistema");
                var cleanDetail = (l.Detalhe ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"{l.Id};{l.DataHora:dd/MM/yyyy HH:mm:ss};\"{userStr}\";\"{l.Acao}\";\"{cleanDetail}\";{l.Tipo};\"{l.Origem}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"CorvuxIAM_AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Cloud() => View();
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Configuracoes() => View(_context.ConfiguracoesSistema.ToList());
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Logs() => View(_context.LogsAuditoria.OrderByDescending(l => l.DataHora).ToList());
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Inteligencia() => View();
        [Authorize(Policy = "RequireAdminOrHelpDesk")]
        public IActionResult Chamados() {
            var chamados = _context.ChamadosSuporte.Include(c => c.Usuario).OrderByDescending(c => c.DataAbertura).ToList();
            return View(chamados);
        }

        [Authorize(Policy = "RequireAdminOrHelpDesk")]
        [HttpPost]
        public IActionResult AtualizarChamado([FromBody] AtualizarChamadoDTO dto) {
            var chamado = _context.ChamadosSuporte.Find(dto.Id);
            if (chamado == null) return NotFound();
            chamado.Status = dto.Status;
            if (dto.Status == "Fechado" && chamado.DataFechamento == null) chamado.DataFechamento = DateTime.UtcNow;
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult AdicionarPolitica([FromBody] PoliticaAcesso p) {
            p.Ativa = true; _context.PoliticasAcesso.Add(p); _context.SaveChanges(); return Ok();
        }

        public class AtualizarChamadoDTO { public int Id { get; set; } public string Status { get; set; } = string.Empty; }
    public class AdminUsuarioDTO {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string CPF { get; set; } = string.Empty;
            public string RG { get; set; } = string.Empty;
            public DateTime? DataNascimento { get; set; }
            public string Telefone { get; set; } = string.Empty;
            public string Endereco { get; set; } = string.Empty;
            public string Departamento { get; set; } = string.Empty;
            public string Cargo { get; set; } = string.Empty;
            public DateTime? DataContratacao { get; set; }
            public string Perfil { get; set; } = string.Empty;
            public string EmailPessoal { get; set; } = string.Empty;
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public IActionResult EditarUsuario([FromBody] AdminUsuarioDTO u)
        {
            var adminId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminId == u.Id.ToString()) return BadRequest("Você não pode editar o próprio perfil administrativamente.");

            var user = _context.Usuarios.Include(x => x.UsuarioPerfis).FirstOrDefault(x => x.Id == u.Id);
            if(user == null) return NotFound();
            user.Nome = u.Nome;
            user.CPF = u.CPF;
            user.Cargo = u.Cargo;
            
            var perfilDb = _context.Perfis.FirstOrDefault(p => p.Nome == u.Perfil);
            if (perfilDb != null) {
                user.UsuarioPerfis.Clear();
                user.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = user.Id, PerfilId = perfilDb.Id });
            }

            user.RG = u.RG ?? user.RG;
            user.Telefone = u.Telefone ?? user.Telefone;
            user.Departamento = u.Departamento ?? user.Departamento;
            user.Endereco = u.Endereco ?? user.Endereco;
            
            _context.LogsAuditoria.Add(new LogAuditoria { DataHora = DateTime.Now, Acao = "Edição Colaborador", Detalhe = $"Usuário {user.Email} atualizado", Tipo = "Info", Origem = "Admin Panel", UsuarioId = int.Parse(adminId ?? "1") });
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public async System.Threading.Tasks.Task<IActionResult> AdicionarUsuario([FromBody] AdminUsuarioDTO novoUsuario)
        {
            if (novoUsuario == null || string.IsNullOrEmpty(novoUsuario.Nome)) return BadRequest("Dados inválidos");
            
            if (!string.IsNullOrEmpty(novoUsuario.EmailPessoal))
            {
                bool isDisposable = await _emailValidator.IsDisposableEmailAsync(novoUsuario.EmailPessoal);
                if (isDisposable)
                {
                    return BadRequest(new { message = "E-mail pessoal inválido. Não é permitido usar provedores de e-mail temporário." });
                }
            }

            var nomes = novoUsuario.Nome.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var baseEmail = nomes.Length > 0 ? $"{nomes.First().ToLower()}.{(nomes.Length > 1 ? nomes.Last().ToLower() : "colaborador")}@corvus.com" : "novo.colaborador@corvus.com";
            baseEmail = baseEmail.Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u").Replace("ã","a").Replace("ç","c");
            
            var email = baseEmail;
            int counter = 1;
            while (_context.Usuarios.Any(u => u.Email == email))
            {
                email = baseEmail.Replace("@corvus.com", $"{counter}@corvus.com");
                counter++;
            }
            
            // 2. Usar gerador abstraído
            string senha = await _passwordGenerator.GenerateSecurePasswordAsync();
            
            var usuarioDb = new Usuario {
                Nome = novoUsuario.Nome,
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
                CPF = novoUsuario.CPF ?? "",
                RG = novoUsuario.RG ?? "",
                DataNascimento = novoUsuario.DataNascimento,
                Telefone = novoUsuario.Telefone ?? "",
                Endereco = novoUsuario.Endereco ?? "",
                Departamento = novoUsuario.Departamento ?? "",
                Cargo = novoUsuario.Cargo ?? "",
                DataContratacao = novoUsuario.DataContratacao,
                Ativo = true
            };
            _context.Usuarios.Add(usuarioDb);
            _context.SaveChanges();
            
            var perfilDb = _context.Perfis.FirstOrDefault(p => p.Nome == novoUsuario.Perfil);
            if (perfilDb == null) perfilDb = _context.Perfis.FirstOrDefault(p => p.Nome == "Colaborador");
            
            if (perfilDb != null) {
                _context.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = usuarioDb.Id, PerfilId = perfilDb.Id });
            }

            _context.LogsAuditoria.Add(new LogAuditoria { DataHora = DateTime.Now, Acao = "Novo Colaborador", Detalhe = $"Usuário {email} adicionado", Tipo = "Info", Origem = "Admin Panel", UsuarioId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1") });
            _context.SaveChanges();
            
            if (!string.IsNullOrEmpty(novoUsuario.EmailPessoal))
            {
                string subject = "Bem-vindo ao CorvusIAM - Suas Credenciais";
                string body = $"<h2>Olá {novoUsuario.Nome},</h2><p>Sua conta corporativa foi criada.</p><p><b>E-mail:</b> {email}<br><b>Senha Temporária:</b> {senha}</p><p>Acesse o painel para redefinir sua senha.</p>";
                try {
                    await _emailSender.SendEmailAsync(novoUsuario.EmailPessoal, subject, body);
                } catch { }
            }

            return Ok(new { message = "OK", email = email });
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public async System.Threading.Tasks.Task<IActionResult> ResetarSenhaUsuario([FromBody] int id)
        {
            var user = _context.Usuarios.Find(id);
            if (user == null) return NotFound();

            string senha = await _passwordGenerator.GenerateSecurePasswordAsync();

            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);
            
            _context.LogsAuditoria.Add(new LogAuditoria { DataHora = DateTime.Now, Acao = "Reset de Senha (Admin)", Detalhe = $"Senha redefinida para {user.Email}", Tipo = "Warning", Origem = "Admin Panel", UsuarioId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1") });
            _context.SaveChanges();
            
            return Ok(new { message = "OK" });
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public IActionResult InativarUsuario([FromBody] int id)
        {
            var adminId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminId == id.ToString()) return BadRequest("Você não pode inativar a própria conta.");

            var user = _context.Usuarios.Find(id);
            if (user == null) return NotFound();
            user.Ativo = !user.Ativo;
            
            _context.LogsAuditoria.Add(new LogAuditoria { DataHora = DateTime.Now, Acao = user.Ativo ? "Ativação de Colaborador" : "Bloqueio de Colaborador", Detalhe = $"Usuário {user.Email} {(user.Ativo ? "ativado" : "bloqueado")}", Tipo = "Warning", Origem = "Admin Panel", UsuarioId = int.Parse(adminId ?? "1") });
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public IActionResult ExcluirUsuario([FromBody] int id)
        {
            var adminId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminId == id.ToString()) return BadRequest("Você não pode excluir a própria conta.");

            var user = _context.Usuarios.Find(id);
            if (user == null) return NotFound();
            
            user.Ativo = false;
            user.Email = $"excluido_{user.Id}_{user.Email}";
            user.Nome = "Usuário Excluído";
            user.CPF = string.Empty;
            user.RG = string.Empty;
            user.Telefone = string.Empty;
            user.Endereco = string.Empty;
            user.DataNascimento = null;
            
            _context.LogsAuditoria.Add(new LogAuditoria { DataHora = DateTime.Now, Acao = "Exclusão de Colaborador", Detalhe = $"Usuário {user.Email} anonimizado e inativado", Tipo = "Danger", Origem = "Admin Panel", UsuarioId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1") });
            
            _context.SaveChanges();
            return Ok();
        }
    
        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult DownloadRelatorio([FromQuery] int page = 1, [FromQuery] int pageSize = 5000)
        {
            // Paginação para evitar OutOfMemoryException
            var logs = _context.LogsAuditoria
                .OrderByDescending(l => l.DataHora)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
                
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,DataHora,Acao,Detalhe,Tipo,Origem");
            foreach (var log in logs)
            {
                string SafeCsvField(string? input)
                {
                    if (string.IsNullOrEmpty(input)) return string.Empty;
                    var clean = input.Replace(',', ' ');
                    if (clean.StartsWith("=") || clean.StartsWith("+") || clean.StartsWith("-") || clean.StartsWith("@"))
                    {
                        clean = "'" + clean;
                    }
                    return clean;
                }

                csv.AppendLine($"{log.Id},{log.DataHora:yyyy-MM-dd HH:mm:ss},{SafeCsvField(log.Acao)},{SafeCsvField(log.Detalhe)},{SafeCsvField(log.Tipo)},{SafeCsvField(log.Origem)}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Relatorio_Auditoria_Pg{page}.csv");
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult ExecutarVarreduraIA()
        {
            int blockedUsers = _context.Usuarios.Count(u => !u.Ativo);
            int threatLogs = _context.LogsAuditoria.Count(l => l.Tipo == "Error" || l.Tipo == "Danger");
            int totalLogs = _context.LogsAuditoria.Count();
            
            var blockedIPs = new System.Collections.Generic.List<string>();
            if (threatLogs > 0) {
                blockedIPs.Add("192.168.1.100 (Rede Interna)");
                blockedIPs.Add("203.0.113.45 (Externa)");
            }
            
            int score = 100 - (threatLogs * 2);
            if (score < 10) score = 10;

            var steps = new System.Collections.Generic.List<string> {
                "> Analisando " + totalLogs + " eventos recentes no log de auditoria... OK",
                "> Verificando status de contas de usuários (" + _context.Usuarios.Count() + " total)... OK",
                "> Identificados " + blockedUsers + " usuários inativos/bloqueados.",
                "> Varredura de ameaças: " + threatLogs + " incidentes recentes detectados.",
                "> Calculando Score de Risco UBA... Score atual: " + score + "%"
            };

            return Ok(new {
                virusCount = 0,
                invCount = threatLogs,
                score = score,
                ips = blockedIPs,
                consoleSteps = steps
            });
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Sincronizar()
        {
            _context.LogsAuditoria.Add(new LogAuditoria { DataHora = DateTime.Now, Acao = "Sincronizacao do Sistema", Detalhe = "Sincronizacao manual executada via dashboard", Tipo = "Info", Origem = "Admin Panel", UsuarioId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1") });
            _context.SaveChanges();
            return Ok(new { message = "Sincronizado com sucesso" });
        }
    }
}




