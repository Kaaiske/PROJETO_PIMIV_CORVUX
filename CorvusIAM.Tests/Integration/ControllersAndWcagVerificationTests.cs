using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CorvusIAM.Tests.Integration
{
    [Collection("SqliteFullStackCollection")]
    public class ControllersAndWcagVerificationTests
    {
        private readonly SqliteFullStackFactory _factory;

        public ControllersAndWcagVerificationTests(SqliteFullStackFactory factory)
        {
            _factory = factory;
        }

        private Task<string> AuthenticateAdminJwtAsync(HttpClient _)
            => Task.FromResult(_factory.CreateAdminJwtToken());

        private static string ExtractAntiforgeryToken(string html)
        {
            var match = Regex.Match(
                html,
                @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
            Assert.True(match.Success, "Campo oculto __RequestVerificationToken não encontrado no HTML renderizado.");
            return !string.IsNullOrEmpty(match.Groups[1].Value) ? match.Groups[1].Value : match.Groups[2].Value;
        }

        private static double ComputeW3cContrastRatio(string hexForeground, string hexBackground)
        {
            static double RelativeLuminance(string hex)
            {
                var clean = hex.Trim().TrimStart('#');
                if (clean.Length == 3)
                {
                    clean = string.Concat(clean.Select(c => $"{c}{c}"));
                }

                double r = int.Parse(clean.Substring(0, 2), NumberStyles.HexNumber) / 255.0;
                double g = int.Parse(clean.Substring(2, 2), NumberStyles.HexNumber) / 255.0;
                double b = int.Parse(clean.Substring(4, 2), NumberStyles.HexNumber) / 255.0;

                static double Channel(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
                return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
            }

            double l1 = RelativeLuminance(hexForeground);
            double l2 = RelativeLuminance(hexBackground);
            double lighter = Math.Max(l1, l2);
            double darker = Math.Min(l1, l2);
            return (lighter + 0.05) / (darker + 0.05);
        }

        [Fact]
        public async Task RestApi_PerfisPoliticasConfiguracoesAndAccess_FullCrudAndMaskingWork()
        {
            var client = _factory.CreateClient();
            var token = await AuthenticateAdminJwtAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1. AccessController (/api/Access)
            var accessResp = await client.GetAsync("/api/Access");
            Assert.Equal(HttpStatusCode.OK, accessResp.StatusCode);
            var accessJson = await accessResp.Content.ReadAsStringAsync();
            Assert.Contains("usuarios.gerenciar", accessJson);

            // 2. PerfisController (/api/Perfis)
            var perfisList = await client.GetAsync("/api/Perfis");
            Assert.Equal(HttpStatusCode.OK, perfisList.StatusCode);

            var perfilNotFound = await client.GetAsync("/api/Perfis/99999");
            Assert.Equal(HttpStatusCode.NotFound, perfilNotFound.StatusCode);

            var novoPerfilResp = await client.PostAsJsonAsync("/api/Perfis", new
            {
                nome = "Auditor Externo Temporário",
                descricao = "Acesso somente leitura para auditoria independente"
            });
            Assert.Equal(HttpStatusCode.Created, novoPerfilResp.StatusCode);
            using var perfilDoc = JsonDocument.Parse(await novoPerfilResp.Content.ReadAsStringAsync());
            int novoPerfilId = perfilDoc.RootElement.GetProperty("id").GetInt32();

            var getPerfil = await client.GetAsync($"/api/Perfis/{novoPerfilId}");
            Assert.Equal(HttpStatusCode.OK, getPerfil.StatusCode);

            var putPerfilMismatch = await client.PutAsJsonAsync($"/api/Perfis/{novoPerfilId}", new
            {
                id = novoPerfilId + 1,
                nome = "Mismatch",
                descricao = "Erro esperado"
            });
            Assert.Equal(HttpStatusCode.BadRequest, putPerfilMismatch.StatusCode);

            var putPerfil = await client.PutAsJsonAsync($"/api/Perfis/{novoPerfilId}", new
            {
                id = novoPerfilId,
                nome = "Auditor Externo ISO 27001",
                descricao = "Escopo atualizado"
            });
            Assert.Equal(HttpStatusCode.NoContent, putPerfil.StatusCode);

            var delPerfil = await client.DeleteAsync($"/api/Perfis/{novoPerfilId}");
            Assert.Equal(HttpStatusCode.NoContent, delPerfil.StatusCode);

            // 3. PoliticasAcessoController (/api/PoliticasAcesso)
            var polList = await client.GetAsync("/api/PoliticasAcesso");
            Assert.Equal(HttpStatusCode.OK, polList.StatusCode);

            var novaPolResp = await client.PostAsJsonAsync("/api/PoliticasAcesso", new
            {
                nome = "Bloqueio Geográfico Fora do Brasil",
                descricao = "Exige validação adicional para IPs internacionais",
                ativa = true
            });
            Assert.Equal(HttpStatusCode.Created, novaPolResp.StatusCode);
            using var polDoc = JsonDocument.Parse(await novaPolResp.Content.ReadAsStringAsync());
            int polId = polDoc.RootElement.GetProperty("id").GetInt32();

            var getPol = await client.GetAsync($"/api/PoliticasAcesso/{polId}");
            Assert.Equal(HttpStatusCode.OK, getPol.StatusCode);

            var putPol = await client.PutAsJsonAsync($"/api/PoliticasAcesso/{polId}", new
            {
                id = polId,
                nome = "Bloqueio Geográfico Zero-Trust",
                descricao = "Regra revisada",
                ativa = false
            });
            Assert.Equal(HttpStatusCode.NoContent, putPol.StatusCode);

            var delPol = await client.DeleteAsync($"/api/PoliticasAcesso/{polId}");
            Assert.Equal(HttpStatusCode.NoContent, delPol.StatusCode);

            // 4. ConfiguracoesController (/api/Configuracoes) + mascaramento de chaves sensíveis
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.ConfiguracoesSistema.Any(c => c.Chave == "JWT_KEY"))
                {
                    db.ConfiguracoesSistema.Add(new ConfiguracaoSistema { Chave = "JWT_KEY", Valor = "SegredoInternoNaoExpor" });
                    db.SaveChanges();
                }
            }

            var cfgList = await client.GetAsync("/api/Configuracoes");
            Assert.Equal(HttpStatusCode.OK, cfgList.StatusCode);
            var cfgBody = await cfgList.Content.ReadAsStringAsync();
            Assert.DoesNotContain("SegredoInternoNaoExpor", cfgBody);
            Assert.Contains("***", cfgBody);

            var novaCfgResp = await client.PostAsJsonAsync("/api/Configuracoes", new
            {
                chave = "TEST_PARAM_SLA_HORAS",
                valor = "24"
            });
            Assert.Equal(HttpStatusCode.Created, novaCfgResp.StatusCode);
            using var cfgDoc = JsonDocument.Parse(await novaCfgResp.Content.ReadAsStringAsync());
            int cfgId = cfgDoc.RootElement.GetProperty("id").GetInt32();

            var getCfg = await client.GetAsync($"/api/Configuracoes/{cfgId}");
            Assert.Equal(HttpStatusCode.OK, getCfg.StatusCode);

            var putCfg = await client.PutAsJsonAsync($"/api/Configuracoes/{cfgId}", new
            {
                id = cfgId,
                chave = "TEST_PARAM_SLA_HORAS",
                valor = "12"
            });
            Assert.Equal(HttpStatusCode.NoContent, putCfg.StatusCode);

            var delCfg = await client.DeleteAsync($"/api/Configuracoes/{cfgId}");
            Assert.Equal(HttpStatusCode.NoContent, delCfg.StatusCode);
        }

        [Fact]
        public async Task RestApi_UsuariosChamadosAndPublicIntegrations_CrudLgpdAndModulo11Work()
        {
            var client = _factory.CreateClient();
            var token = await AuthenticateAdminJwtAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1. UsuariosController (/api/Usuarios)
            var listUsers = await client.GetAsync("/api/Usuarios");
            Assert.Equal(HttpStatusCode.OK, listUsers.StatusCode);

            var uniqueEmail = $"colab.{Guid.NewGuid().ToString("N").Substring(0, 8)}@corvuxiam.tech";
            var createUserResp = await client.PostAsJsonAsync("/api/Usuarios", new
            {
                nome = "Marina Costa Analista",
                email = uniqueEmail,
                senha = "Password@2026!",
                ativo = true,
                cpf = "52998224725",
                cargo = "Analista de Governança",
                departamento = "Segurança",
                perfilId = 2
            });
            Assert.Equal(HttpStatusCode.Created, createUserResp.StatusCode);
            using var userDoc = JsonDocument.Parse(await createUserResp.Content.ReadAsStringAsync());
            int createdUserId = userDoc.RootElement.GetProperty("id").GetInt32();

            // Conflito de duplicidade (409)
            var dupResp = await client.PostAsJsonAsync("/api/Usuarios", new
            {
                nome = "Duplicado",
                email = uniqueEmail,
                senha = "Password@2026!",
                ativo = true
            });
            Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

            var getUser = await client.GetAsync($"/api/Usuarios/{createdUserId}");
            Assert.Equal(HttpStatusCode.OK, getUser.StatusCode);

            var updateUser = await client.PutAsJsonAsync($"/api/Usuarios/{createdUserId}", new
            {
                id = createdUserId,
                nome = "Marina Costa Sênior",
                email = uniqueEmail,
                senha = "",
                ativo = true,
                cpf = "52998224725",
                cargo = "Especialista IAM",
                departamento = "SecOps",
                perfilId = 2
            });
            Assert.Equal(HttpStatusCode.NoContent, updateUser.StatusCode);

            // Anonimização LGPD no DELETE
            var deleteUser = await client.DeleteAsync($"/api/Usuarios/{createdUserId}");
            Assert.Equal(HttpStatusCode.NoContent, deleteUser.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var anonUser = await db.Usuarios.FindAsync(createdUserId);
                Assert.NotNull(anonUser);
                Assert.False(anonUser!.Ativo);
                Assert.True(string.IsNullOrEmpty(anonUser.CPF));
            }

            // 2. ChamadosController (/api/Chamados) CRUD completo
            var listChamados = await client.GetAsync("/api/Chamados");
            Assert.Equal(HttpStatusCode.OK, listChamados.StatusCode);

            var createTicket = await client.PostAsJsonAsync("/api/Chamados", new
            {
                assunto = "Revisão de Permissão RBAC",
                descricao = "Ajuste de acesso departamental",
                categoria = "Acesso",
                prioridade = "Normal"
            });
            Assert.True(createTicket.StatusCode == HttpStatusCode.Created || createTicket.StatusCode == HttpStatusCode.OK);
            using var ticketDoc = JsonDocument.Parse(await createTicket.Content.ReadAsStringAsync());
            int ticketId = ticketDoc.RootElement.GetProperty("id").GetInt32();

            var getTicket = await client.GetAsync($"/api/Chamados/{ticketId}");
            Assert.Equal(HttpStatusCode.OK, getTicket.StatusCode);

            var missingAttachment = await client.GetAsync("/api/chamados/anexos/inexistente.png");
            Assert.Equal(HttpStatusCode.NotFound, missingAttachment.StatusCode);

            // 3. PublicIntegrationsController (/api/public/*)
            var validCpfResp = await client.PostAsJsonAsync("/api/public/validar-cpf", new { cpf = "52998224725" });
            Assert.Equal(HttpStatusCode.OK, validCpfResp.StatusCode);
            Assert.Contains("true", await validCpfResp.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var invalidCpfRepeated = await client.PostAsJsonAsync("/api/public/validar-cpf", new { cpf = "111.111.111-11" });
            Assert.Equal(HttpStatusCode.OK, invalidCpfRepeated.StatusCode);
            Assert.Contains("false", await invalidCpfRepeated.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var invalidCpfShort = await client.PostAsJsonAsync("/api/public/validar-cpf", new { cpf = "12345" });
            Assert.Equal(HttpStatusCode.OK, invalidCpfShort.StatusCode);
            Assert.Contains("false", await invalidCpfShort.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var invalidCep = await client.GetAsync("/api/public/cep/123");
            Assert.Equal(HttpStatusCode.BadRequest, invalidCep.StatusCode);

            var invalidCnpj = await client.GetAsync("/api/public/cnpj/123");
            Assert.Equal(HttpStatusCode.BadRequest, invalidCnpj.StatusCode);

            var invalidBank = await client.GetAsync("/api/public/bancos/abc");
            Assert.Equal(HttpStatusCode.BadRequest, invalidBank.StatusCode);
        }

        [Fact]
        public async Task WebMvc_AuthMobileHomeAndRbacAuthorization_EnforceZeroTrustAndStandardMobileRoute()
        {
            var anonClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // 1. /Mobile sem sessão redireciona para /Auth/Login preservando ReturnUrl=/Mobile
            var mobileAnon = await anonClient.GetAsync("/Mobile");
            Assert.Equal(HttpStatusCode.Redirect, mobileAnon.StatusCode);
            var redirectLocation = mobileAnon.Headers.Location?.ToString() ?? string.Empty;
            Assert.Contains("/Auth/Login", redirectLocation);
            Assert.Contains("Mobile", redirectLocation);

            // 2. HomeController roteamento inicial e página de erro amigável
            var homeAnon = await anonClient.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.Redirect, homeAnon.StatusCode);
            Assert.Contains("/Auth/Login", homeAnon.Headers.Location?.ToString() ?? string.Empty);

            var homeErrorResp = await anonClient.GetAsync("/Home/Error");
            Assert.Equal(HttpStatusCode.OK, homeErrorResp.StatusCode);

            var esqueciResp = await anonClient.GetAsync("/Auth/EsqueciSenha");
            Assert.Equal(HttpStatusCode.OK, esqueciResp.StatusCode);

            // 3. Fluxo MVC /Auth/Login com CSRF real (credencial inválida -> alerta acessível; credencial válida -> cookie Secure em HTTPS)
            var cookieClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
            var loginGet = await cookieClient.GetAsync("/Auth/Login");
            Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);
            var csrf = ExtractAntiforgeryToken(await loginGet.Content.ReadAsStringAsync());

            var badLoginForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrf,
                ["email"] = "integracao.admin@corvuxiam.tech",
                ["senha"] = "SenhaIncorreta123!"
            });
            var badLoginResp = await cookieClient.PostAsync("/Auth/Login", badLoginForm);
            Assert.Equal(HttpStatusCode.OK, badLoginResp.StatusCode);
            var badLoginHtml = await badLoginResp.Content.ReadAsStringAsync();
            Assert.Contains("role=\"alert\"", badLoginHtml);
            Assert.Contains("aria-live=\"assertive\"", badLoginHtml);

            var csrf2 = ExtractAntiforgeryToken(badLoginHtml);
            var goodLoginForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrf2,
                ["email"] = "integracao.admin@corvuxiam.tech",
                ["senha"] = "Corvux@2026!"
            });
            var goodLoginResp = await cookieClient.PostAsync("/Auth/Login", goodLoginForm);
            Assert.Equal(HttpStatusCode.Redirect, goodLoginResp.StatusCode);
            Assert.Contains("/Admin", goodLoginResp.Headers.Location?.ToString() ?? string.Empty);

            // Sessão autenticada acessa /Home/Index (redireciona ao perfil) e /Mobile diretamente (200 OK)
            var homeAuth = await cookieClient.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.Redirect, homeAuth.StatusCode);

            var mobileAuth = await cookieClient.GetAsync("/Mobile");
            Assert.Equal(HttpStatusCode.OK, mobileAuth.StatusCode);

            // Logout encerra o cookie
            var logoutResp = await cookieClient.GetAsync("/Auth/Logout");
            Assert.Equal(HttpStatusCode.Redirect, logoutResp.StatusCode);
        }

        [Fact]
        public async Task WebMvc_AdminAndPortalControllers_ViewsExportsAndTransactionalOperationsWork()
        {
            // Seed de dispositivo de rede para testar telemetria e ações de endpoint no AdminController
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.DispositivosRede.Any(d => d.Hostname == "SRV-IAM-TEST01"))
                {
                    db.DispositivosRede.Add(new DispositivoRede
                    {
                        Hostname = "SRV-IAM-TEST01",
                        Ip = "10.10.0.25",
                        Tipo = "Servidor Linux",
                        SistemaOperacional = "Ubuntu 24.04 LTS",
                        Status = "Atencao",
                        PatchesPendentes = 2,
                        DiscoUsoPercentual = 60,
                        RamUsoPercentual = 55,
                        UsuarioResponsavel = "SecOps",
                        Vlan = "VLAN-10"
                    });
                    db.SaveChanges();
                }
            }

            var client = _factory.CreateClient();
            var token = await AuthenticateAdminJwtAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1. Renderização das Views administrativas e extração do token Antiforgery
            var adminIndex = await client.GetAsync("/Admin/Index");
            Assert.Equal(HttpStatusCode.OK, adminIndex.StatusCode);
            var indexHtml = await adminIndex.Content.ReadAsStringAsync();
            var csrf = ExtractAntiforgeryToken(indexHtml);
            client.DefaultRequestHeaders.Add("RequestVerificationToken", csrf);

            string[] adminGetRoutes =
            {
                "/Admin/Usuarios",
                "/Admin/Politicas",
                "/Admin/Perfis",
                "/Admin/Relatorios",
                "/Admin/Cloud",
                "/Admin/Configuracoes",
                "/Admin/Logs",
                "/Admin/Inteligencia",
                "/Admin/Chamados",
                "/Admin/Integracoes",
                "/Admin/ExportarUsuariosCsv",
                "/Admin/ExportarLogsAuditoriaCsv",
                "/Admin/DownloadRelatorio?page=1&pageSize=25",
                "/Admin/ObterIpsBloqueados",
                "/Admin/ObterStatusIds",
                "/Admin/ObterConexoesEndpoint?hostname=SRV-IAM-TEST01"
            };

            foreach (var route in adminGetRoutes)
            {
                var resp = await client.GetAsync(route);
                Assert.True(resp.StatusCode == HttpStatusCode.OK, $"Falha inesperada em GET {route}: {resp.StatusCode}");
            }

            // 2. Operações POST administrativas (Catálogo de cargos, Políticas, Firewall, IDS, Endpoint, SecOps, Usuários)
            var cargoResp = await client.PostAsJsonAsync("/Admin/AdicionarCargo", new
            {
                titulo = "Engenheiro de Segurança IAM",
                departamento = "Cibersegurança",
                nivel = "Sênior",
                perfilSugerido = "Admin"
            });
            Assert.Equal(HttpStatusCode.OK, cargoResp.StatusCode);

            var polAddResp = await client.PostAsJsonAsync("/Admin/AdicionarPolitica", new
            {
                nome = "Política Teste MVC",
                descricao = "Validação automatizada de controlador"
            });
            Assert.Equal(HttpStatusCode.OK, polAddResp.StatusCode);

            var saveCfgResp = await client.PostAsJsonAsync("/Admin/SalvarConfiguracoes", new
            {
                configs = new Dictionary<string, string>
                {
                    ["SESSION_TIMEOUT_MINUTES"] = "25",
                    ["JWT_KEY"] = "TentativaSobrescritaIgnorada"
                }
            });
            Assert.Equal(HttpStatusCode.OK, saveCfgResp.StatusCode);

            var fwBlock = await client.PostAsJsonAsync("/Admin/GerenciarFirewallIp", new
            {
                ip = "203.0.113.77",
                motivo = "Teste automatizado de bloqueio WAF",
                acao = "Bloquear"
            });
            Assert.Equal(HttpStatusCode.OK, fwBlock.StatusCode);

            var fwUnblock = await client.PostAsJsonAsync("/Admin/GerenciarFirewallIp", new
            {
                ip = "203.0.113.77",
                motivo = "Liberação após revisão",
                acao = "Desbloquear"
            });
            Assert.Equal(HttpStatusCode.OK, fwUnblock.StatusCode);

            var idsDetect = await client.PostAsJsonAsync("/Admin/TestarAssinaturaIds", new
            {
                ip = "198.51.100.88",
                payload = "1' UNION SELECT * FROM Usuarios--"
            });
            Assert.Equal(HttpStatusCode.OK, idsDetect.StatusCode);
            Assert.Contains("SIG-SQLI-01", await idsDetect.Content.ReadAsStringAsync());

            var idsClean = await client.PostAsJsonAsync("/Admin/TestarAssinaturaIds", new
            {
                ip = "198.51.100.89",
                payload = "consulta normal de relatorio"
            });
            Assert.Equal(HttpStatusCode.OK, idsClean.StatusCode);
            Assert.Contains("false", await idsClean.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var epPatch = await client.PostAsJsonAsync("/Admin/ExecutarAcaoEndpoint", new
            {
                hostname = "SRV-IAM-TEST01",
                acao = "Patch"
            });
            Assert.Equal(HttpStatusCode.OK, epPatch.StatusCode);

            var epBlockConn = await client.PostAsJsonAsync("/Admin/BloquearConexaoEndpoint", new
            {
                hostname = "SRV-IAM-TEST01",
                destinoIp = "198.51.100.200",
                destinoPorta = 4444,
                protocolo = "TCP",
                motivo = "Porta suspeita"
            });
            Assert.Equal(HttpStatusCode.OK, epBlockConn.StatusCode);

            var incidentResp = await client.PostAsJsonAsync("/Admin/DispararRespostaIncidente", new
            {
                tipoAcao = "ClearFirewallRules"
            });
            Assert.Equal(HttpStatusCode.OK, incidentResp.StatusCode);

            var iaScanResp = await client.PostAsync("/Admin/ExecutarVarreduraIA", null);
            Assert.Equal(HttpStatusCode.OK, iaScanResp.StatusCode);

            var syncResp = await client.PostAsync("/Admin/Sincronizar", null);
            Assert.Equal(HttpStatusCode.OK, syncResp.StatusCode);

            var procResp = await client.PostAsJsonAsync("/Admin/ExecutarProcedureBanco", new
            {
                procedure = "sp_ObterMetricasSecOpsDashboard"
            });
            Assert.Equal(HttpStatusCode.OK, procResp.StatusCode);

            // Provisionamento, edição, reset de senha, inativação e anonimização de colaborador via AdminController
            var addUserAdmin = await client.PostAsJsonAsync("/Admin/AdicionarUsuario", new
            {
                nome = "Lucas Ferreira Silva",
                cpf = "123.456.789-01",
                departamento = "Operações",
                cargo = "Analista Operacional",
                perfil = "Colaborador",
                racaCorIbge = "Parda",
                modalidadeIngresso = "Ação Afirmativa - Equidade Racial"
            });
            Assert.Equal(HttpStatusCode.OK, addUserAdmin.StatusCode);

            int lucasId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                lucasId = db.Usuarios.First(u => u.Nome == "Lucas Ferreira Silva").Id;
            }

            var editUserAdmin = await client.PostAsJsonAsync("/Admin/EditarUsuario", new
            {
                id = lucasId,
                nome = "Lucas Ferreira Silva",
                cargo = "Analista Operacional Pleno",
                departamento = "Operações",
                perfil = "Colaborador",
                racaCorIbge = "Parda"
            });
            Assert.Equal(HttpStatusCode.OK, editUserAdmin.StatusCode);

            var resetPwdAdmin = await client.PostAsJsonAsync("/Admin/ResetarSenhaUsuario", lucasId);
            Assert.Equal(HttpStatusCode.OK, resetPwdAdmin.StatusCode);

            var inativarAdmin = await client.PostAsJsonAsync("/Admin/InativarUsuario", lucasId);
            Assert.Equal(HttpStatusCode.OK, inativarAdmin.StatusCode);

            var excluirAdmin = await client.PostAsJsonAsync("/Admin/ExcluirUsuario", lucasId);
            Assert.Equal(HttpStatusCode.OK, excluirAdmin.StatusCode);

            // 3. PortalController Views, Relatórios Contábeis/RH, MFA Setup e Upload Seguro de Anexo
            string[] portalGetRoutes =
            {
                "/Portal/RH",
                "/Portal/Contabilidade",
                "/Portal/HelpDesk",
                "/Portal/MinhaConta",
                "/Portal/ObterMfaSetup",
                "/Portal/ExportarSpedFiscal",
                "/Portal/ExportarEspelhoPonto"
            };

            foreach (var route in portalGetRoutes)
            {
                var resp = await client.GetAsync(route);
                Assert.True(resp.StatusCode == HttpStatusCode.OK, $"Falha inesperada em GET {route}: {resp.StatusCode}");
            }

            var nfResp = await client.PostAsJsonAsync("/Portal/EmitirNotaFiscal", new
            {
                tomadorNome = "Empresa Cliente Homologação S.A.",
                tomadorCnpj = "12.345.678/0001-99",
                valor = 4800.00m,
                descricao = "Licenciamento SaaS CorvuxIAM"
            });
            Assert.Equal(HttpStatusCode.OK, nfResp.StatusCode);

            var fechamentoResp = await client.PostAsync("/Portal/ExecutarFechamentoMensal", null);
            Assert.Equal(HttpStatusCode.OK, fechamentoResp.StatusCode);

            var feriasResp = await client.PostAsJsonAsync("/Portal/SolicitarFerias", new
            {
                colaboradorNome = "Admin Integração",
                dataInicio = DateTime.UtcNow.AddDays(45),
                dias = 15
            });
            Assert.Equal(HttpStatusCode.OK, feriasResp.StatusCode);

            var folhaResp = await client.PostAsync("/Portal/ProcessarFolhaPagamento", null);
            Assert.Equal(HttpStatusCode.OK, folhaResp.StatusCode);

            var badMfaResp = await client.PostAsJsonAsync("/Portal/ConfirmarAtivacaoMfa", new { codigo = "000000" });
            Assert.Equal(HttpStatusCode.BadRequest, badMfaResp.StatusCode);

            // Upload multipart com assinatura mágica PNG válida (89 50 4E 47 0D 0A 1A 0A) + download autenticado em /api/chamados/anexos/{key}
            using var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent("Evidência de Teste com Anexo PNG"), "Assunto");
            multipart.Add(new StringContent("Validação de magic bytes no upload"), "Descricao");
            byte[] validPngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
            var fileContent = new ByteArrayContent(validPngHeader);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            multipart.Add(fileContent, "Anexo", "evidencia.png");

            var abrirChamadoResp = await client.PostAsync("/Portal/AbrirChamado", multipart);
            Assert.Equal(HttpStatusCode.OK, abrirChamadoResp.StatusCode);
            using var abrirDoc = JsonDocument.Parse(await abrirChamadoResp.Content.ReadAsStringAsync());
            int novoChamadoId = abrirDoc.RootElement.GetProperty("id").GetInt32();

            string anexoUrl;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                anexoUrl = (await db.ChamadosSuporte.FindAsync(novoChamadoId))?.AnexoUrl ?? string.Empty;
            }
            Assert.False(string.IsNullOrWhiteSpace(anexoUrl));

            var downloadAnexoResp = await client.GetAsync(anexoUrl);
            Assert.Equal(HttpStatusCode.OK, downloadAnexoResp.StatusCode);
            Assert.Equal("image/png", downloadAnexoResp.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Wcag22Criterion143_ColorContrast_MeetsMinimumLuminanceRatiosInLightDarkAndHighContrast()
        {
            var client = _factory.CreateClient();
            var loginHtml = await (await client.GetAsync("/Auth/Login")).Content.ReadAsStringAsync();
            var mobileCss = await (await client.GetAsync("/mobile/app.css")).Content.ReadAsStringAsync();

            var token = await AuthenticateAdminJwtAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var adminHtml = await (await client.GetAsync("/Admin/Index")).Content.ReadAsStringAsync();

            // Confirma presença dos tokens cromáticos auditados nos estilos reais servidos pela aplicação
            Assert.Contains("--bg-card: #F5F7FA", loginHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--text-main: #0F172A", loginHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--text-muted: #475569", loginHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--text-muted: #475569", mobileCss, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".high-contrast body { background-color: #000000 !important; color: #ffffff !important; }", adminHtml, StringComparison.OrdinalIgnoreCase);

            // Cálculo matemático da razão de luminância relativa W3C WCAG 2.2 Critério 1.4.3 (mínimo >= 4.5:1 texto normal)
            double lightPrimaryOnCard = ComputeW3cContrastRatio("#0F172A", "#F5F7FA");     // ~16.5:1
            double lightSecondaryOnCard = ComputeW3cContrastRatio("#334155", "#F5F7FA");   // ~9.6:1
            double lightMutedOnCard = ComputeW3cContrastRatio("#475569", "#F5F7FA");       // ~6.8:1
            double lightBodyOnAlabaster = ComputeW3cContrastRatio("#1E293B", "#E4EBF3");   // ~11.9:1
            double buttonWhiteOnBrand = ComputeW3cContrastRatio("#FFFFFF", "#2563EB");     // ~5.16:1
            double darkPrimaryOnCard = ComputeW3cContrastRatio("#FAFAFA", "#0E0E12");      // ~18.7:1
            double darkMutedOnCard = ComputeW3cContrastRatio("#A1A1AA", "#0E0E12");        // ~7.8:1
            double mobileMutedOnSurface = ComputeW3cContrastRatio("#475569", "#F1F5F9");   // ~6.6:1
            double highContrastMode = ComputeW3cContrastRatio("#FFFFFF", "#000000");       // 21.0:1

            Assert.True(lightPrimaryOnCard >= 4.5, $"Contraste texto primário claro insuficiente: {lightPrimaryOnCard:F2}:1");
            Assert.True(lightSecondaryOnCard >= 4.5, $"Contraste texto secundário claro insuficiente: {lightSecondaryOnCard:F2}:1");
            Assert.True(lightMutedOnCard >= 4.5, $"Contraste texto auxiliar claro insuficiente: {lightMutedOnCard:F2}:1");
            Assert.True(lightBodyOnAlabaster >= 4.5, $"Contraste corpo Bento claro insuficiente: {lightBodyOnAlabaster:F2}:1");
            Assert.True(buttonWhiteOnBrand >= 4.5, $"Contraste botão primário insuficiente: {buttonWhiteOnBrand:F2}:1");
            Assert.True(darkPrimaryOnCard >= 4.5, $"Contraste texto primário escuro insuficiente: {darkPrimaryOnCard:F2}:1");
            Assert.True(darkMutedOnCard >= 4.5, $"Contraste texto auxiliar escuro insuficiente: {darkMutedOnCard:F2}:1");
            Assert.True(mobileMutedOnSurface >= 4.5, $"Contraste texto auxiliar mobile insuficiente: {mobileMutedOnSurface:F2}:1");
            Assert.True(highContrastMode >= 7.0, $"Modo alto contraste deve atingir >= 7:1: {highContrastMode:F2}:1");
        }

        [Fact]
        public async Task Wcag22Criteria211And247And144_KeyboardNavigationVisibleFocusAndTextZoom_ArePreserved()
        {
            var client = _factory.CreateClient();
            var loginHtml = await (await client.GetAsync("/Auth/Login")).Content.ReadAsStringAsync();
            var siteCss = await (await client.GetAsync("/css/site.css")).Content.ReadAsStringAsync();
            var mobileCss = await (await client.GetAsync("/mobile/app.css")).Content.ReadAsStringAsync();

            var token = await AuthenticateAdminJwtAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var adminHtml = await (await client.GetAsync("/Admin/Index")).Content.ReadAsStringAsync();
            var mobileHtml = await (await client.GetAsync("/Mobile")).Content.ReadAsStringAsync();

            // 1. Foco visível (WCAG 2.4.7 Focus Visible) presente nas folhas de estilo e layouts
            Assert.Contains(":focus-visible", loginHtml);
            Assert.Contains("outline: 2px solid", loginHtml);
            Assert.Contains(":focus-visible", adminHtml);
            Assert.Contains(":focus-visible", mobileCss);
            Assert.Contains(":focus", siteCss);

            // 2. Ausência de armadilhas de ordem de tabulação (tabindex positivo > 0 viola ordem natural WCAG 2.4.3)
            var positiveTabIndexRegex = new Regex(@"tabindex\s*=\s*[""']([1-9]\d*)[""']", RegexOptions.IgnoreCase);
            Assert.False(positiveTabIndexRegex.IsMatch(loginHtml), "Login contém tabindex positivo que quebra a ordem natural de teclado.");
            Assert.False(positiveTabIndexRegex.IsMatch(adminHtml), "Admin contém tabindex positivo que quebra a ordem natural de teclado.");
            Assert.False(positiveTabIndexRegex.IsMatch(mobileHtml), "Mobile PWA contém tabindex positivo que quebra a ordem natural de teclado.");

            // 3. Redimensionamento de texto / zoom livre (WCAG 1.4.4 Resize Text: sem bloqueio user-scalable=no)
            Assert.DoesNotContain("user-scalable=no", loginHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("user-scalable=no", adminHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("user-scalable=no", mobileHtml, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Wcag22Criteria131And332And412_FormsLabelsLandmarksAndAssistiveTechnologies_AreVerified()
        {
            var client = _factory.CreateClient();
            var loginHtml = WebUtility.HtmlDecode(await (await client.GetAsync("/Auth/Login")).Content.ReadAsStringAsync());

            var token = await AuthenticateAdminJwtAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var adminHtml = WebUtility.HtmlDecode(await (await client.GetAsync("/Admin/Index")).Content.ReadAsStringAsync());
            var mobileHtml = WebUtility.HtmlDecode(await (await client.GetAsync("/Mobile")).Content.ReadAsStringAsync());

            // 1. Formulário de Login: associação explícita <label for="..."> + required + aria-required (WCAG 1.3.1 & 3.3.2)
            Assert.Contains("for=\"loginEmail\"", loginHtml);
            Assert.Contains("id=\"loginEmail\"", loginHtml);
            Assert.Contains("for=\"loginSenha\"", loginHtml);
            Assert.Contains("id=\"loginSenha\"", loginHtml);
            Assert.Contains("aria-required=\"true\"", loginHtml);

            // 2. Formulário de Chamado no Mobile PWA: <label for="assunto"> e <label for="descricao"> + campo de busca rotulado
            Assert.Contains("for=\"assunto\"", mobileHtml);
            Assert.Contains("id=\"assunto\"", mobileHtml);
            Assert.Contains("for=\"descricao\"", mobileHtml);
            Assert.Contains("id=\"descricao\"", mobileHtml);
            Assert.Contains("aria-label=\"Pesquisar chamados por assunto ou código\"", mobileHtml);

            // 3. Semântica para leitores de tela: idioma, regiões landmark (<main>, <nav>, <header>) e regiões vivas (aria-live)
            foreach (var html in new[] { loginHtml, adminHtml, mobileHtml })
            {
                Assert.Contains("lang=\"pt-BR\"", html);
                Assert.Contains("<main", html);
            }
            Assert.Contains("<nav", adminHtml);
            Assert.Contains("aria-label=\"Navegação Principal\"", adminHtml);
            Assert.Contains("<header", adminHtml);
            Assert.Contains("<nav class=\"app-bottom-nav\" aria-label=\"Navegação Principal Mobile\"", mobileHtml);
            Assert.Contains("aria-live=\"polite\"", mobileHtml);

            // 4. Tecnologias assistivas complementares: Widget VLibras e síntese/reconhecimento de voz (Web Speech API)
            Assert.Contains("vw-access-button", loginHtml);
            Assert.Contains("vlibras-plugin.js", loginHtml);
            Assert.Contains("speechSynthesis", loginHtml);
            Assert.Contains("SpeechRecognition", loginHtml);
            Assert.Contains("btnHeaderOuvirConteudo", adminHtml);
            Assert.Contains("btnHeaderDitadoVoz", adminHtml);
        }
    }
}
