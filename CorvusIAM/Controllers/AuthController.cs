using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CorvusIAM.Data;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Extensions.Caching.Memory;

namespace CorvusIAM.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITurnstileService _turnstileService;
        private readonly IIpGeolocationService _geoService;
        private readonly IEmailSenderService _emailSender;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly ITotpService _totpService;

        public AuthController(
            ApplicationDbContext context, 
            ITurnstileService turnstileService, 
            IIpGeolocationService geoService,
            IEmailSenderService emailSender,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
            ITotpService totpService)
        {
            _context = context;
            _turnstileService = turnstileService;
            _geoService = geoService;
            _emailSender = emailSender;
            _cache = cache;
            _totpService = totpService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
        public async System.Threading.Tasks.Task<IActionResult> Login(string email, string senha)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";

            // Verificação ativa de regras de Firewall WAF
            if (_context.FirewallRegras.Any(f => f.Ip == ip && f.Ativo))
            {
                Serilog.Log.Warning("Acesso bloqueado por regra ativa do Firewall WAF para o IP: {IP}", ip);
                ViewBag.Erro = "Acesso bloqueado: Seu endereço IP consta na lista de bloqueios ativos do Firewall Corporativo.";
                return View();
            }

            var cacheKey = $"login_fails_{ip}";
            if (_cache.TryGetValue(cacheKey, out int fails) && fails >= 10)
            {
                ViewBag.Erro = "Muitas tentativas falhas deste IP. Tente novamente mais tarde.";
                return View();
            }

            // Validação do Cloudflare Turnstile
            var turnstileToken = Request.Form["cf-turnstile-response"].ToString();
            var isHuman = await _turnstileService.VerifyTokenAsync(turnstileToken);
            if (!isHuman)
            {
                ViewBag.Erro = "Validação de segurança falhou (Anti-bot). Por favor, tente novamente.";
                return View();
            }

            var user = _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .ThenInclude(p => p.PerfilPermissoes)
                .ThenInclude(pp => pp.Permissao)
                .FirstOrDefault(u => u.Email == email);
                
            if (user == null)
            {
                Log.Warning("Tentativa de login falhou (Usuário não existe) para {Email}. IP: {IP}", email, ip);
                _cache.Set(cacheKey, fails + 1, TimeSpan.FromMinutes(15));
                ViewBag.Erro = "Usuário ou senha inválidos.";
                return View();
            }

            // Verifica se está inativo
            if (!user.Ativo)
            {
                ViewBag.Erro = "Sua conta está inativa. Contate o administrador.";
                return View();
            }

            // Verifica se a conta está temporariamente bloqueada
            if (user.DataDesbloqueio.HasValue && user.DataDesbloqueio.Value > DateTime.UtcNow)
            {
                ViewBag.Erro = $"Conta bloqueada por excesso de tentativas. Tente novamente após {user.DataDesbloqueio.Value.ToLocalTime():HH:mm}.";
                return View();
            }


            // Verifica senha
            if (BCrypt.Net.BCrypt.Verify(senha, user.SenhaHash))
            {
                // Reseta falhas do IP e do usuário
                _cache.Remove(cacheKey);
                user.TentativasLogin = 0;
                user.DataDesbloqueio = null;
                await _context.SaveChangesAsync();

                // Autenticação em Duas Etapas (MFA / 2FA com TOTP)
                if (user.MfaHabilitado && !string.IsNullOrEmpty(user.MfaSecretKey))
                {
                    var mfaSessionToken = Guid.NewGuid().ToString("N");
                    _cache.Set($"mfa_user_{mfaSessionToken}", user.Id, TimeSpan.FromMinutes(5));
                    return RedirectToAction("ValidarMfa", "Auth", new { token = mfaSessionToken });
                }

                var role = user.UsuarioPerfis.FirstOrDefault()?.Perfil?.Nome ?? "Colaborador";
                
                var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Nome),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
                };

                foreach (var permissao in user.UsuarioPerfis
                    .SelectMany(up => up.Perfil?.PerfilPermissoes ?? Array.Empty<Entities.PerfilPermissao>())
                    .Select(pp => pp.Permissao?.Nome)
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Distinct())
                {
                    claims.Add(new System.Security.Claims.Claim("permission", permissao!));
                }

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
                
                await HttpContext.SignInAsync("Cookies", claimsPrincipal);

                // Registrar auditoria de sucesso
                _context.LogsAuditoria.Add(new Entities.LogAuditoria
                {
                    UsuarioId = user.Id,
                    Acao = "Login Efetuado",
                    Detalhe = $"Login bem-sucedido via Web. IP: {ip}",
                    Tipo = "Autenticação",
                    Origem = "Web WebApp"
                });
                await _context.SaveChangesAsync();

                var location = await _geoService.GetLocationFromIpAsync(ip);
                Log.Information("Login bem-sucedido: Usuário {Email}, Perfil: {Role}. IP: {IP} - Localização: {Location}", user.Email, role, ip, location);

                if (role.Contains("Admin")) return RedirectToAction("Index", "Admin");
                if (role.Contains("Humanos")) return RedirectToAction("Usuarios", "Admin");
                if (role.Contains("Contabilidade")) return RedirectToAction("Contabilidade", "Portal");
                if (role.Contains("Help")) return RedirectToAction("Chamados", "Admin");
                
                return RedirectToAction("MinhaConta", "Portal");
            }
            else
            {
                // Tratamento de falha de senha atrelado ao IP
                fails++;
                _cache.Set(cacheKey, fails, TimeSpan.FromMinutes(15));

                // Incrementar tentativas na conta do usuário
                user.TentativasLogin++;
                if (user.TentativasLogin >= 5)
                {
                    user.DataDesbloqueio = DateTime.UtcNow.AddMinutes(15);
                    _context.LogsAuditoria.Add(new Entities.LogAuditoria
                    {
                        UsuarioId = user.Id,
                        Acao = "Bloqueio de Conta",
                        Detalhe = $"Conta bloqueada por 15 minutos após 5 tentativas falhas. IP: {ip}",
                        Tipo = "Segurança",
                        Origem = "Web WebApp"
                    });
                }

                // Registrar auditoria de falha
                _context.LogsAuditoria.Add(new Entities.LogAuditoria
                {
                    UsuarioId = user.Id,
                    Acao = "Falha de Login",
                    Detalhe = $"Senha inválida. Falha do IP. IP: {ip}",
                    Tipo = "Autenticação",
                    Origem = "Web WebApp"
                });
                await _context.SaveChangesAsync();

                Log.Warning("Tentativa de login falhou (Senha incorreta) para {Email}. IP: {IP}", email, ip);
                
                if (user.TentativasLogin >= 5)
                {
                    ViewBag.Erro = "Muitas tentativas falhas. Sua conta foi bloqueada por 15 minutos.";
                }
                else if (fails >= 10)
                {
                    ViewBag.Erro = "Muitas tentativas falhas deste IP. Bloqueio temporário ativado.";
                }
                else
                {
                    ViewBag.Erro = "Usuário ou senha inválidos.";
                }

                return View();
            }
        }
            [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Auth");
        }
        [HttpGet]
        public IActionResult EsqueciSenha()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> EsqueciSenha(string email)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                var token = System.Guid.NewGuid().ToString();
                user.ResetSenhaToken = token;
                user.ResetSenhaExpiracao = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                var resetLink = Url.Action("RedefinirSenha", "Auth", new { token, email }, Request.Scheme);
                
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Redefinição de Senha - Corvus IAM",
                    $"Clique no link para redefinir sua senha: {resetLink}");
            }

            ViewBag.Mensagem = "Se o e-mail existir em nossa base, um link de recuperação foi enviado.";
            return View();
        }

        [HttpGet]
        public IActionResult RedefinirSenha(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return BadRequest("Token ou E-mail inválido.");

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> RedefinirSenha(string token, string email, string novaSenha)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.ResetSenhaToken == token);
            
            if (user == null || !user.ResetSenhaExpiracao.HasValue || user.ResetSenhaExpiracao.Value < DateTime.UtcNow)
            {
                ViewBag.Erro = "Token inválido ou expirado.";
                return View();
            }

            if (novaSenha.Length < 8 || !novaSenha.Any(char.IsUpper) || !novaSenha.Any(char.IsLower) || !novaSenha.Any(char.IsDigit) || !novaSenha.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                ViewBag.Token = token;
                ViewBag.Email = email;
                ViewBag.Erro = "A senha deve conter no mínimo 8 caracteres, maiúsculas, minúsculas, números e símbolos especiais.";
                return View();
            }

            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            user.ResetSenhaToken = string.Empty;
            user.ResetSenhaExpiracao = null;
            user.TentativasLogin = 0;
            user.DataDesbloqueio = null;

            _context.LogsAuditoria.Add(new Entities.LogAuditoria
            {
                UsuarioId = user.Id,
                Acao = "Redefinição de Senha",
                Detalhe = "Senha redefinida via token de recuperação.",
                Tipo = "Segurança",
                Origem = "Web WebApp"
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("Login", new { mensagem = "Senha redefinida com sucesso. Faça o login." });
        }

        [HttpGet]
        public IActionResult ValidarMfa(string token)
        {
            if (string.IsNullOrEmpty(token) || !_cache.TryGetValue($"mfa_user_{token}", out int _))
            {
                return RedirectToAction("Login", "Auth");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> ValidarMfa(string token, string codigo)
        {
            if (string.IsNullOrEmpty(token) || !_cache.TryGetValue($"mfa_user_{token}", out int userId))
            {
                ViewBag.Erro = "Sessão de verificação expirada. Faça o login novamente.";
                return View();
            }

            var user = _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .ThenInclude(p => p.PerfilPermissoes)
                .ThenInclude(pp => pp.Permissao)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null) return RedirectToAction("Login", "Auth");

            bool valido = _totpService.ValidateCode(user.MfaSecretKey, codigo);
            if (!valido)
            {
                ViewBag.Token = token;
                ViewBag.Erro = "Código de verificação incorreto ou expirado. Tente novamente.";
                return View();
            }

            // Remove o token de MFA temporário
            _cache.Remove($"mfa_user_{token}");

            var role = user.UsuarioPerfis.FirstOrDefault()?.Perfil?.Nome ?? "Colaborador";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";

            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Nome),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
            };

            foreach (var permissao in user.UsuarioPerfis
                .SelectMany(up => up.Perfil?.PerfilPermissoes ?? Array.Empty<Entities.PerfilPermissao>())
                .Select(pp => pp.Permissao?.Nome)
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct())
            {
                claims.Add(new System.Security.Claims.Claim("permission", permissao!));
            }

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
            
            await HttpContext.SignInAsync("Cookies", claimsPrincipal);

            _context.LogsAuditoria.Add(new Entities.LogAuditoria
            {
                UsuarioId = user.Id,
                Acao = "Login MFA Efetuado",
                Detalhe = $"Login com validação de segundo fator (TOTP) bem-sucedido. IP: {ip}",
                Tipo = "Autenticação",
                Origem = "Web WebApp"
            });
            await _context.SaveChangesAsync();

            if (role.Contains("Admin")) return RedirectToAction("Index", "Admin");
            if (role.Contains("Humanos")) return RedirectToAction("Usuarios", "Admin");
            if (role.Contains("Contabilidade")) return RedirectToAction("Contabilidade", "Portal");
            if (role.Contains("Help")) return RedirectToAction("Chamados", "Admin");
            
            return RedirectToAction("MinhaConta", "Portal");
        }
    }
}

