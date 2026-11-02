using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CorvusIAM.Tests.Integration
{
    public class SqliteFullStackFactory : WebApplicationFactory<Program>, IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteFullStackFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("Environment", "Development");
            builder.UseSetting("Jwt:Key", "TestJwtKey_FullStackIntegration_2026_SecureKey32Chars!");
            builder.UseSetting("JWT_KEY", "TestJwtKey_FullStackIntegration_2026_SecureKey32Chars!");
            builder.UseSetting("Jwt:Issuer", "CorvusIAM");
            builder.UseSetting("Jwt:Audience", "CorvusIAMUsers");

            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(DbContextOptions<SqliteDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType == typeof(SqliteDbContext))
                    .ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                if (!db.Usuarios.Any(u => u.Email == "integracao.admin@corvuxiam.tech"))
                {
                    var admin = new Usuario
                    {
                        Nome = "Admin Integração",
                        Email = "integracao.admin@corvuxiam.tech",
                        SenhaHash = BCrypt.Net.BCrypt.HashPassword("Corvux@2026!"),
                        Ativo = true,
                        CPF = "987.654.321-09",
                        Cargo = "Arquiteto IAM",
                        Departamento = "Segurança da Informação"
                    };
                    db.Usuarios.Add(admin);
                    db.SaveChanges();

                    if (db.Perfis.Any(p => p.Id == 1))
                    {
                        db.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = admin.Id, PerfilId = 1 });
                        db.SaveChanges();
                    }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    public class FullStackIntegrationAndAccessibilityTests : IClassFixture<SqliteFullStackFactory>
    {
        private readonly SqliteFullStackFactory _factory;

        public FullStackIntegrationAndAccessibilityTests(SqliteFullStackFactory factory)
        {
            _factory = factory;
        }

        private async Task<(string Token, string RefreshToken)> AuthenticateAdminAsync(HttpClient client)
        {
            var loginResp = await client.PostAsJsonAsync("/api/Auth/login", new
            {
                username = "integracao.admin@corvuxiam.tech",
                password = "Corvux@2026!"
            });
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

            using var doc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
            var token = doc.RootElement.GetProperty("token").GetString();
            var refresh = doc.RootElement.GetProperty("refreshToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.False(string.IsNullOrWhiteSpace(refresh));
            return (token!, refresh!);
        }

        [Fact]
        public async Task MobileAndApiAuth_LoginRefreshRotationAndLogoutRevocation_WorksEndToEnd()
        {
            var client = _factory.CreateClient();
            var (token, firstRefreshToken) = await AuthenticateAdminAsync(client);

            // 1. Consulta de perfil autenticado via JWT (/api/Usuarios/me - usado pelo React Native)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var meResp = await client.GetAsync("/api/Usuarios/me");
            Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);
            var meBody = await meResp.Content.ReadAsStringAsync();
            Assert.Contains("integracao.admin@corvuxiam.tech", meBody);

            // 2. Rotação de Refresh Token (uso único Zero-Trust)
            var refreshResp = await client.PostAsJsonAsync("/api/Auth/refresh-token", new
            {
                email = "integracao.admin@corvuxiam.tech",
                refreshToken = firstRefreshToken
            });
            Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

            using var refreshDoc = JsonDocument.Parse(await refreshResp.Content.ReadAsStringAsync());
            var secondRefreshToken = refreshDoc.RootElement.GetProperty("refreshToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(secondRefreshToken));
            Assert.NotEqual(firstRefreshToken, secondRefreshToken);

            // Reuso do Refresh Token anterior deve ser negado imediatamente
            var replayResp = await client.PostAsJsonAsync("/api/Auth/refresh-token", new
            {
                email = "integracao.admin@corvuxiam.tech",
                refreshToken = firstRefreshToken
            });
            Assert.Equal(HttpStatusCode.Unauthorized, replayResp.StatusCode);

            // 3. Logout revoga o Refresh Token atual no banco de dados
            var logoutResp = await client.PostAsJsonAsync("/api/Auth/logout", new
            {
                email = "integracao.admin@corvuxiam.tech",
                refreshToken = secondRefreshToken
            });
            Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);

            var afterLogoutResp = await client.PostAsJsonAsync("/api/Auth/refresh-token", new
            {
                email = "integracao.admin@corvuxiam.tech",
                refreshToken = secondRefreshToken
            });
            Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResp.StatusCode);
        }

        [Fact]
        public async Task MobilePwaAndOfflineSync_ManifestViewAndIdempotentTicketCreation_PreventsDuplicates()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // 1. Redirecionamento seguro de login mobile
            var mobileLoginRedirect = await client.GetAsync("/Mobile/Login");
            Assert.Equal(HttpStatusCode.Redirect, mobileLoginRedirect.StatusCode);
            Assert.Contains("/Auth/Login", mobileLoginRedirect.Headers.Location?.ToString() ?? string.Empty);

            // 2. Manifesto PWA servido com MIME type oficial
            var manifestResp = await client.GetAsync("/mobile/manifest.webmanifest");
            Assert.Equal(HttpStatusCode.OK, manifestResp.StatusCode);
            Assert.Equal("application/manifest+json", manifestResp.Content.Headers.ContentType?.MediaType);

            // 3. Interface Mobile PWA autenticada e sincronização offline idempotente
            var authClient = _factory.CreateClient();
            var (token, _) = await AuthenticateAdminAsync(authClient);
            authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var mobileViewResp = await authClient.GetAsync("/Mobile");
            Assert.Equal(HttpStatusCode.OK, mobileViewResp.StatusCode);
            var mobileHtml = await mobileViewResp.Content.ReadAsStringAsync();
            Assert.Contains("lang=\"pt-BR\"", mobileHtml);
            Assert.Contains("manifest.webmanifest", mobileHtml);
            Assert.Contains("aria-label=", mobileHtml);

            var idempotencyKey = "pwa-sync-" + Guid.NewGuid().ToString("N");
            var payload = new
            {
                assunto = "Sincronização Offline PWA",
                descricao = "Chamado enfileirado no IndexedDB durante queda de rede",
                categoria = "Rede",
                prioridade = "Alta",
                clientIdempotencyKey = idempotencyKey
            };

            using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/Chamados") { Content = JsonContent.Create(payload) };
            req1.Headers.Add("X-Idempotency-Key", idempotencyKey);
            var res1 = await authClient.SendAsync(req1);
            Assert.True(res1.StatusCode == HttpStatusCode.Created || res1.StatusCode == HttpStatusCode.OK);

            using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/Chamados") { Content = JsonContent.Create(payload) };
            req2.Headers.Add("X-Idempotency-Key", idempotencyKey);
            var res2 = await authClient.SendAsync(req2);
            Assert.True(res2.StatusCode == HttpStatusCode.Created || res2.StatusCode == HttpStatusCode.OK || res2.StatusCode == HttpStatusCode.Conflict);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await db.ChamadosSuporte.CountAsync(c => c.ClientIdempotencyKey == idempotencyKey);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task InboundGatewayV1_EnforcesAuthorizationAndDataMinimizationLgpd()
        {
            var unauthClient = _factory.CreateClient();
            var pingResp = await unauthClient.GetAsync("/api/v1/ping");
            Assert.Equal(HttpStatusCode.OK, pingResp.StatusCode);
            Assert.Contains("ONLINE", await pingResp.Content.ReadAsStringAsync());

            var deniedResp = await unauthClient.GetAsync("/api/v1/users");
            Assert.Equal(HttpStatusCode.Unauthorized, deniedResp.StatusCode);

            var authClient = _factory.CreateClient();
            var (token, _) = await AuthenticateAdminAsync(authClient);
            authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var usersResp = await authClient.GetAsync("/api/v1/users?page=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, usersResp.StatusCode);
            var json = await usersResp.Content.ReadAsStringAsync();

            // Garante minimização de dados (LGPD Art. 6º, III): sem exposição de hash de senha, CPF, RG ou segredo MFA
            Assert.DoesNotContain("senhaHash", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("mfaSecretKey", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("987.654.321-09", json, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("/api/v1/ping?filtro=1%20UNION%20SELECT%20*%20FROM%20Usuarios")]
        [InlineData("/api/v1/ping?busca=%3Cscript%3Ealert(1)%3C%2Fscript%3E")]
        [InlineData("/api/v1/ping?arquivo=..%2F..%2Fetc%2Fpasswd")]
        public async Task WafLayer7_BlocksSqliXssAndPathTraversalAndPersistsAuditLog(string maliciousUrl)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync(maliciousUrl);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Corvux WAF L7", body);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(await db.LogsAuditoria.AnyAsync(l => l.Origem == "Corvux L7 WAF" && l.Tipo == "Bloqueio"));
        }

        [Fact]
        public async Task HealthSecurityHeadersAndWebAccessibility_ComplyWithZeroTrustAndWcagStructure()
        {
            var client = _factory.CreateClient();

            var healthResp = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, healthResp.StatusCode);
            Assert.Equal("Healthy", await healthResp.Content.ReadAsStringAsync());

            Assert.Equal("nosniff", healthResp.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
            Assert.Equal("DENY", healthResp.Headers.GetValues("X-Frame-Options").FirstOrDefault());
            Assert.True(healthResp.Headers.Contains("Content-Security-Policy"));
            Assert.True(healthResp.Headers.Contains("Referrer-Policy"));

            var loginResp = await client.GetAsync("/Auth/Login");
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var html = await loginResp.Content.ReadAsStringAsync();

            // Verificações estruturais de acessibilidade (idioma pt-BR, rótulos semânticos e suporte VLibras)
            Assert.Contains("lang=\"pt-BR\"", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<label", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("vlibras.gov.br", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
