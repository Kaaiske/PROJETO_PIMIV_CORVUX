using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using CorvusIAM.Controllers.Api;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Interfaces;
using CorvusIAM.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CorvusIAM.Tests.Services
{
    public class SecurityAndAuthorizationTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static ClaimsPrincipal CreateUserPrincipal(int userId, string role)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, $"user{userId}@corvus.com")
            }, "TestAuth"));
        }

        [Fact]
        public async Task AnexosController_AcessoAnexoDeOutroUsuario_RetornaForbid()
        {
            // Arrange
            var context = GetDbContext();
            var mockStorage = new Mock<IStorageService>();
            mockStorage.Setup(s => s.GetFileUrl("secret.pdf")).Returns("/uploads/secret.pdf");

            context.ChamadosSuporte.Add(new ChamadoSuporte
            {
                Id = 10,
                UsuarioId = 1, // Pertence ao usuário 1
                Assunto = "Chamado Confidencial",
                Descricao = "Dados sigilosos",
                AnexoUrl = "/uploads/secret.pdf",
                AnexoNomeOriginal = "secret.pdf"
            });
            await context.SaveChangesAsync();

            var controller = new AnexosController(context, mockStorage.Object)
            {
                ControllerContext = new ControllerContext
                {
                    // Usuário 2 tentando baixar o anexo do usuário 1
                    HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(2, "Colaborador") }
                }
            };

            // Act
            var result = await controller.Download("secret.pdf");

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ConfiguracoesController_ChaveSensivel_RetornaValorMascarado()
        {
            // Arrange
            var context = GetDbContext();
            context.ConfiguracoesSistema.Add(new ConfiguracaoSistema
            {
                Id = 1,
                Chave = "JWT_KEY",
                Valor = "SuperSecretKey12345678901234567890"
            });
            context.ConfiguracoesSistema.Add(new ConfiguracaoSistema
            {
                Id = 2,
                Chave = "APP_NAME",
                Valor = "CorvuxIAM"
            });
            await context.SaveChangesAsync();

            var controller = new ConfiguracoesController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(1, "Admin") }
                }
            };

            // Act
            var result = await controller.Listar() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var list = Assert.IsAssignableFrom<List<ConfiguracaoResponseDTO>>(result.Value);
            var jwtConfig = list.Find(c => c.Chave == "JWT_KEY");
            var appConfig = list.Find(c => c.Chave == "APP_NAME");

            Assert.NotNull(jwtConfig);
            Assert.Equal("***", jwtConfig.Valor); // Mascarado
            Assert.NotNull(appConfig);
            Assert.Equal("CorvuxIAM", appConfig.Valor); // Público
        }

        [Fact]
        public async Task ConfiguracoesController_TentativaCriarChaveSensivel_RetornaBadRequest()
        {
            // Arrange
            var context = GetDbContext();
            var controller = new ConfiguracoesController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(1, "Admin") }
                }
            };

            // Act
            var result = await controller.Cadastrar(new ConfiguracaoCreateDTO
            {
                Chave = "API_GATEWAY_TOKEN",
                Valor = "HackerToken"
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void DbDataSeeder_EmProducao_ContasDemoFicamInativasComSenhaAleatoria()
        {
            // Arrange
            var context = GetDbContext();
            var previousEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

                // Pré-insere uma conta demo simulando versão legada
                context.Usuarios.Add(new Usuario
                {
                    Id = 50,
                    Email = "rh@corvus.com",
                    Nome = "Demo RH",
                    SenhaHash = "senhaAntiga",
                    Ativo = true,
                    RefreshToken = "token_ativo"
                });
                context.SaveChanges();

                // Act
                DbDataSeeder.Seed(context);

                // Assert
                var demo = context.Usuarios.FirstOrDefault(u => u.Email == "rh@corvus.com");
                Assert.NotNull(demo);
                Assert.False(demo.Ativo);
                Assert.Empty(demo.RefreshToken);
                Assert.NotEqual("senhaAntiga", demo.SenhaHash);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnv);
            }
        }

        [Fact]
        public async Task ChamadosController_RequisicaoDuplicada_RetornaMesmoIdComDuplicateTrue()
        {
            // Arrange
            var context = GetDbContext();
            var mockAudit = new Mock<ILogAuditoriaService>();
            var idempotencyKey = "client-uuid-" + Guid.NewGuid();

            var controller = new ChamadosController(context, mockAudit.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(1, "Colaborador") }
                }
            };

            var dto = new ChamadoCreateDTO
            {
                Assunto = "Problema com VPN",
                Descricao = "Não consigo conectar à rede interna",
                Categoria = "Rede",
                Prioridade = "Alta",
                ClientIdempotencyKey = idempotencyKey
            };

            // Act 1
            var result1 = await controller.AbrirChamado(dto);
            Assert.IsType<CreatedResult>(result1);

            // Act 2 (Repetição simultânea ou reenvio de rede com mesma chave de idempotência)
            var result2 = await controller.AbrirChamado(dto) as OkObjectResult;

            // Assert
            Assert.NotNull(result2);
            var value = result2.Value;
            var duplicateProp = value.GetType().GetProperty("duplicate");
            Assert.NotNull(duplicateProp);
            Assert.True((bool)duplicateProp.GetValue(value));
        }
    }
}
