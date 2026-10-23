using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CorvusIAM.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace CorvusIAM.Tests.Integration
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Key", "TestJwtKey_SuperSecret1234567890_TestOnly!");
            builder.UseSetting("JWT_KEY", "TestJwtKey_SuperSecret1234567890_TestOnly!");
            builder.UseSetting("ADMIN_INITIAL_PASSWORD", "TestAdminPassword123!");
            builder.UseSetting("Environment", "Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        }
    }

    public class ChamadosApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ChamadosApiTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetMeusChamados_SemToken_RetornaUnauthorized()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var response = await client.GetAsync("/api/chamados");
            // Se usar cookies sem token, ele redireciona (302 Found) ou (401 se JWT puro)
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found);
        }
    }
}


