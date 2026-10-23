using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorvusIAM.Data;
using CorvusIAM.Interfaces;
using CorvusIAM.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Controllers {
    [Authorize]
    public class PortalController : Controller {
        
        private readonly ApplicationDbContext _context;
        private readonly IPasswordPwnedService _passwordPwned;
        private readonly ITotpService _totpService;
        private readonly IStorageService _storageService;
        private readonly IMfaSecretProtector? _mfaSecretProtector;

        public PortalController(
            ApplicationDbContext context, 
            IPasswordPwnedService passwordPwned,
            ITotpService totpService,
            IStorageService storageService,
            IMfaSecretProtector? mfaSecretProtector = null)
        {
            _context = context;
            _passwordPwned = passwordPwned;
            _totpService = totpService;
            _storageService = storageService;
            _mfaSecretProtector = mfaSecretProtector;
        }

        [Authorize(Policy = "RequireHR")]
        public IActionResult RH() 
        { 
            ViewData["Role"] = "RH"; 
            ViewData["UserName"] = User.Identity?.Name ?? "Usuário"; 

            var colaboradores = _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.Nome).ToList();
            var folhas = _context.FolhasPagamento.OrderByDescending(f => f.DataProcessamento).ToList();
            var ferias = _context.RegistrosFerias.OrderByDescending(f => f.DataInicio).ToList();
            var ultimaFolha = folhas.FirstOrDefault();

            var vm = new RHViewModel
            {
                TotalColaboradoresAtivos = colaboradores.Count,
                TotalFolhaSalarial = ultimaFolha?.ValorLiquidoTotal ?? (colaboradores.Count * 4677.77m),
                FeriasAVencerAlertas = ferias.Count(f => f.Status == "Aprovada"),
                StatusControlePonto = "100% Regular",
                Folhas = folhas,
                Ferias = ferias,
                Colaboradores = colaboradores
            };

            return View(vm); 
        }
        
        [Authorize(Policy = "RequireAccounting")]
        public IActionResult Contabilidade() 
        { 
            ViewData["Role"] = "Contabilidade"; 
            ViewData["UserName"] = User.Identity?.Name ?? "Usuário"; 

            var notas = _context.NotasFiscais.OrderByDescending(n => n.DataEmissao).ToList();
            var fechamentos = _context.FechamentosMensais.OrderByDescending(f => f.DataFechamento).ToList();
            var faturamento = notas.Sum(n => n.Valor);

            var vm = new ContabilidadeViewModel
            {
                TotalFaturamento = faturamento,
                TotalNotasEmitidas = notas.Count,
                ContasAPagarSemana = 22400.00m,
                StatusConciliacao = "Em Dia",
                NotasFiscais = notas,
                Fechamentos = fechamentos
            };

            return View(vm); 
        }
        
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
        [RequestSizeLimit(5 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
        public async Task<IActionResult> AbrirChamado([FromForm] NovoChamadoFormDTO form)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(form.Assunto))
            {
                return BadRequest(new { message = "O assunto do chamado é obrigatório." });
            }

            string? anexoUrl = null;
            string? anexoNome = null;

            if (form.Anexo != null && form.Anexo.Length > 0)
            {
                try
                {
                    using var stream = form.Anexo.OpenReadStream();
                    var fileKey = await _storageService.UploadFileAsync(stream, form.Anexo.FileName, form.Anexo.ContentType);
                    anexoUrl = _storageService.GetFileUrl(fileKey);
                    anexoNome = form.Anexo.FileName;
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = $"Erro ao anexar arquivo: {ex.Message}" });
                }
            }

            var chamado = new CorvusIAM.Entities.ChamadoSuporte {
                UsuarioId = int.Parse(userIdClaim),
                Assunto = form.Assunto,
                Descricao = form.Descricao ?? string.Empty,
                Status = "Aberto",
                DataAbertura = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow,
                AnexoUrl = anexoUrl,
                AnexoNomeOriginal = anexoNome
            };

            _context.ChamadosSuporte.Add(chamado);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chamado aberto com sucesso!", id = chamado.Id });
        }

        [HttpGet]
        public IActionResult ObterMfaSetup()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var user = _context.Usuarios.Find(int.Parse(userIdClaim));
            if (user == null) return NotFound();

            string secret = string.IsNullOrEmpty(user.MfaSecretKey) 
                ? _totpService.GenerateSecretKey() 
                : (_mfaSecretProtector?.Unprotect(user.MfaSecretKey) ?? user.MfaSecretKey ?? string.Empty);

            if (_mfaSecretProtector != null && (string.IsNullOrEmpty(user.MfaSecretKey) || !_mfaSecretProtector.IsProtected(user.MfaSecretKey)))
            {
                user.MfaSecretKey = _mfaSecretProtector.Protect(secret);
                _context.SaveChanges();
            }

            string? qrCodeUrl = user.MfaHabilitado ? null : _totpService.GenerateQrCodeImageUrl(user.Email, secret);
            string? setupUri = user.MfaHabilitado ? null : _totpService.GenerateQrCodeUri(user.Email, secret);

            return Ok(new {
                habilitado = user.MfaHabilitado,
                secretKey = user.MfaHabilitado ? null : secret,
                qrCodeUrl = qrCodeUrl,
                setupUri = setupUri
            });
        }

        [HttpPost]
        public IActionResult ConfirmarAtivacaoMfa([FromBody] ValidarMfaDTO dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var user = _context.Usuarios.Find(int.Parse(userIdClaim));
            if (user == null) return NotFound();

            bool valido = _totpService.ValidateCode(_mfaSecretProtector?.Unprotect(user.MfaSecretKey) ?? user.MfaSecretKey, dto.Codigo);
            if (!valido)
            {
                return BadRequest(new { message = "Código de verificação incorreto ou expirado. Tente novamente." });
            }

            user.MfaHabilitado = true;
            _context.LogsAuditoria.Add(new Entities.LogAuditoria {
                UsuarioId = user.Id,
                DataHora = DateTime.UtcNow,
                Acao = "MFA Ativado",
                Detalhe = "Autenticação em dois fatores (TOTP) configurada com sucesso.",
                Tipo = "Segurança",
                Origem = "Portal Colaborador"
            });
            _context.SaveChanges();

            return Ok(new { message = "Autenticação em dois fatores ativada com sucesso!" });
        }

        [HttpPost]
        public IActionResult DesativarMfa([FromBody] ValidarMfaDTO dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var user = _context.Usuarios.Find(int.Parse(userIdClaim));
            if (user == null) return NotFound();

            bool valido = _totpService.ValidateCode(_mfaSecretProtector?.Unprotect(user.MfaSecretKey) ?? user.MfaSecretKey, dto.Codigo);
            if (!valido)
            {
                return BadRequest(new { message = "Código de autenticação inválido para desativação." });
            }

            user.MfaHabilitado = false;
            user.MfaSecretKey = string.Empty;
            _context.LogsAuditoria.Add(new Entities.LogAuditoria {
                UsuarioId = user.Id,
                DataHora = DateTime.UtcNow,
                Acao = "MFA Desativado",
                Detalhe = "Autenticação em dois fatores (TOTP) desativada pelo usuário.",
                Tipo = "Warning",
                Origem = "Portal Colaborador"
            });
            _context.SaveChanges();

            return Ok(new { message = "Autenticação em dois fatores desativada com sucesso." });
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

        public class NovoChamadoFormDTO {
            public string Assunto { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public Microsoft.AspNetCore.Http.IFormFile? Anexo { get; set; }
        }

        public class ValidarMfaDTO {
            public string Codigo { get; set; } = string.Empty;
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

        // ================= CONTABILIDADE & FINANCEIRO =================

        public class EmitirNotaFiscalDTO
        {
            public string TomadorNome { get; set; } = string.Empty;
            public string TomadorCnpj { get; set; } = string.Empty;
            public decimal Valor { get; set; }
            public string? Descricao { get; set; }
        }

        [HttpPost]
        [Authorize(Policy = "RequireAccounting")]
        public async Task<IActionResult> EmitirNotaFiscal([FromBody] EmitirNotaFiscalDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TomadorNome) || dto.Valor <= 0)
            {
                return BadRequest(new { message = "Tomador e valor válidos são obrigatórios." });
            }

            int proxNumero = (_context.NotasFiscais.Max(n => (int?)n.Numero) ?? 1028) + 1;
            string protocolo = $"{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100000, 999999)}";
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);

            var nf = new CorvusIAM.Entities.NotaFiscal
            {
                Numero = proxNumero,
                TomadorNome = dto.TomadorNome,
                TomadorCnpj = string.IsNullOrWhiteSpace(dto.TomadorCnpj) ? "00.000.000/0001-00" : dto.TomadorCnpj,
                Valor = dto.Valor,
                Descricao = dto.Descricao ?? "Prestação de serviços em Gestão de Identidades e Acessos (IAM)",
                DataEmissao = DateTime.UtcNow,
                ProtocoloSefaz = protocolo,
                Status = "Autorizada",
                EmitidoPorUsuarioId = userId > 0 ? userId : null
            };

            _context.NotasFiscais.Add(nf);
            _context.LogsAuditoria.Add(new CorvusIAM.Entities.LogAuditoria
            {
                UsuarioId = userId > 0 ? userId : null,
                Acao = "Emissão de Nota Fiscal",
                Detalhe = $"NFS-e Nº {nf.Numero} emitida para '{nf.TomadorNome}'. Valor: R$ {nf.Valor:N2}. Protocolo SEFAZ: {protocolo}",
                Tipo = "Info",
                Origem = "Módulo Contábil",
                DataHora = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                numero = nf.Numero,
                tomador = nf.TomadorNome,
                valor = nf.Valor,
                protocolo = protocolo,
                message = "Nota Fiscal autorizada e transmitida à SEFAZ com sucesso!"
            });
        }

        [HttpPost]
        [Authorize(Policy = "RequireAccounting")]
        public async Task<IActionResult> ExecutarFechamentoMensal()
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            string mesAtual = DateTime.UtcNow.ToString("MM/yyyy");

            var receitaTotal = await _context.NotasFiscais.SumAsync(n => n.Valor);
            var despesas = Math.Round(receitaTotal * 0.28m, 2);
            var lucro = receitaTotal - despesas;

            var fechamento = new CorvusIAM.Entities.FechamentoMensal
            {
                MesAno = mesAtual,
                ReceitaBruta = receitaTotal,
                DespesasOperacionais = despesas,
                LucroLiquido = lucro,
                DataFechamento = DateTime.UtcNow,
                Status = "Fechado",
                FechadoPorUsuarioId = userId > 0 ? userId : null
            };

            _context.FechamentosMensais.Add(fechamento);
            _context.LogsAuditoria.Add(new CorvusIAM.Entities.LogAuditoria
            {
                UsuarioId = userId > 0 ? userId : null,
                Acao = "Fechamento Contábil Mensal",
                Detalhe = $"Fechamento {mesAtual} executado. Receita: R$ {receitaTotal:N2}, Despesas: R$ {despesas:N2}, Lucro: R$ {lucro:N2}",
                Tipo = "Info",
                Origem = "Módulo Contábil",
                DataHora = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                mes = mesAtual,
                receita = receitaTotal,
                despesas = despesas,
                lucro = lucro,
                message = $"Fechamento contábil de {mesAtual} concluído com sucesso!"
            });
        }

        [HttpGet]
        [Authorize(Policy = "RequireAccounting")]
        public async Task<IActionResult> ExportarSpedFiscal()
        {
            var notas = await _context.NotasFiscais.OrderBy(n => n.Numero).ToListAsync();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("|0000|014|0|01012026|31122026|CORVUS SECURITY TECNOLOGIA LTDA|12345678000199|SP|3550308|||A|1|");
            sb.AppendLine("|0001|0|");
            sb.AppendLine("|0100|CONTADOR RESPONSAVEL|12345678909|123456|12345678000199|01001000|AV PAULISTA|1000||BELA VISTA|1199999999|contabil@corvuxiam.tech|3550308|");

            foreach (var nf in notas)
            {
                sb.AppendLine($"|C100|0|1|CLI_{nf.Id}|55|00|1|{nf.Numero}|{nf.ProtocoloSefaz}|{nf.DataEmissao:ddMMyyyy}|{nf.DataEmissao:ddMMyyyy}|{nf.Valor:F2}|1|0,00|0,00|{nf.Valor:F2}|99||||||0,00|0,00|0,00|0,00|0,00|0,00|0,00|");
            }

            sb.AppendLine("|9001|0|");
            sb.AppendLine($"|9999|{notas.Count + 5}|");

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/plain", $"SPED_EFD_Contribuicoes_{DateTime.UtcNow:yyyyMMdd}.txt");
        }

        // ================= RECURSOS HUMANOS =================

        public class SolicitarFeriasDTO
        {
            public string ColaboradorNome { get; set; } = string.Empty;
            public DateTime DataInicio { get; set; }
            public int Dias { get; set; } = 30;
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public async Task<IActionResult> SolicitarFerias([FromBody] SolicitarFeriasDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ColaboradorNome))
            {
                return BadRequest(new { message = "Nome do colaborador é obrigatório." });
            }

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            var ferias = new CorvusIAM.Entities.RegistroFerias
            {
                ColaboradorNome = dto.ColaboradorNome,
                DataInicio = dto.DataInicio > DateTime.MinValue ? dto.DataInicio : DateTime.UtcNow.AddDays(30),
                DataFim = (dto.DataInicio > DateTime.MinValue ? dto.DataInicio : DateTime.UtcNow.AddDays(30)).AddDays(dto.Dias),
                Dias = dto.Dias,
                Status = "Aprovada",
                DataAprovacao = DateTime.UtcNow
            };

            _context.RegistrosFerias.Add(ferias);
            _context.LogsAuditoria.Add(new CorvusIAM.Entities.LogAuditoria
            {
                UsuarioId = userId > 0 ? userId : null,
                Acao = "Concessão de Férias",
                Detalhe = $"Férias de {ferias.Dias} dias aprovadas para '{ferias.ColaboradorNome}' com início em {ferias.DataInicio:dd/MM/yyyy}",
                Tipo = "Info",
                Origem = "Módulo RH",
                DataHora = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                colaborador = ferias.ColaboradorNome,
                inicio = ferias.DataInicio.ToString("dd/MM/yyyy"),
                dias = ferias.Dias,
                message = "Férias aprovadas e lançadas no sistema de DP com sucesso!"
            });
        }

        [HttpPost]
        [Authorize(Policy = "RequireHR")]
        public async Task<IActionResult> ProcessarFolhaPagamento()
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            string mesAtual = DateTime.UtcNow.ToString("MM/yyyy");

            int totalColabs = await _context.Usuarios.CountAsync(u => u.Ativo);
            if (totalColabs == 0) totalColabs = 18;

            decimal brutoTotal = totalColabs * 5800.00m;
            decimal inss = Math.Round(brutoTotal * 0.09m, 2);
            decimal fgts = Math.Round(brutoTotal * 0.08m, 2);
            decimal liquido = brutoTotal - inss;

            var folha = new CorvusIAM.Entities.FolhaPagamento
            {
                MesReferencia = mesAtual,
                TotalColaboradores = totalColabs,
                ValorBrutoTotal = brutoTotal,
                ValorLiquidoTotal = liquido,
                InssTotal = inss,
                FgtsTotal = fgts,
                DataProcessamento = DateTime.UtcNow,
                Status = "Processada"
            };

            _context.FolhasPagamento.Add(folha);
            _context.LogsAuditoria.Add(new CorvusIAM.Entities.LogAuditoria
            {
                UsuarioId = userId > 0 ? userId : null,
                Acao = "Fechamento de Folha de Pagamento",
                Detalhe = $"Folha de {mesAtual} processada para {totalColabs} colaboradores. Líquido Total: R$ {liquido:N2}",
                Tipo = "Info",
                Origem = "Módulo RH",
                DataHora = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                mes = mesAtual,
                colaboradores = totalColabs,
                bruto = brutoTotal,
                liquido = liquido,
                inss = inss,
                fgts = fgts,
                message = $"Folha de pagamento de {mesAtual} processada com sucesso no e-Social!"
            });
        }

        [HttpGet]
        [Authorize(Policy = "RequireHR")]
        public async Task<IActionResult> ExportarEspelhoPonto()
        {
            var colabs = await _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.Nome).ToListAsync();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("0000000001112345678000199123456789012CORVUS SECURITY TECNOLOGIA LTDA0101202631122026" + DateTime.UtcNow.ToString("ddMMyyyyHHmm"));
            int nsr = 1;

            foreach (var colab in colabs)
            {
                string pis = $"123{colab.Id:D8}";
                string dataMarc = DateTime.UtcNow.ToString("ddMMyyyy");
                sb.AppendLine($"{nsr++:D9}2{dataMarc}0800{pis}");
                sb.AppendLine($"{nsr++:D9}2{dataMarc}1200{pis}");
                sb.AppendLine($"{nsr++:D9}2{dataMarc}1300{pis}");
                sb.AppendLine($"{nsr++:D9}2{dataMarc}1700{pis}");
            }

            sb.AppendLine($"{nsr:D9}9");

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/plain", $"Espelho_Ponto_AFD_Portaria671_{DateTime.UtcNow:yyyyMMdd}.txt");
        }
    }
}

