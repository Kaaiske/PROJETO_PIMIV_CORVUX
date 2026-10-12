using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Controllers.Api;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models.DTOs;
using System.Collections.Generic;

namespace CorvusIAM.Tests.Controllers.Api
{
    public class UsuariosControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options); context.Perfis.Add(new Perfil { Id = 5, Nome = "Colaborador" }); context.SaveChanges(); return context;
        }

        [Fact]
        public async Task Listar_RetornaListaDeUsuarios()
        {
            // Arrange
            var context = GetDbContext();
            context.Usuarios.Add(new Usuario { Id = 1, Nome = "Test 1", Email = "test1@test.com" });
            context.Usuarios.Add(new Usuario { Id = 2, Nome = "Test 2", Email = "test2@test.com" });
            await context.SaveChangesAsync();

            var controller = new UsuariosController(context, new Moq.Mock<CorvusIAM.Interfaces.ILogAuditoriaService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
                        }, "mock"))
                    }
                }
            };

            // Act
            var result = await controller.Listar() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var items = Assert.IsType<List<UsuarioResponseDTO>>(result.Value);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task Consultar_RetornaNotFound_QuandoNaoExiste()
        {
            var context = GetDbContext();
            var controller = new UsuariosController(context, new Moq.Mock<CorvusIAM.Interfaces.ILogAuditoriaService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
                        }, "mock"))
                    }
                }
            };

            var result = await controller.Consultar(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Consultar_RetornaOk_QuandoExiste()
        {
            var context = GetDbContext();
            context.Usuarios.Add(new Usuario { Id = 1, Nome = "Test", Email = "test@test.com" });
            await context.SaveChangesAsync();
            var controller = new UsuariosController(context, new Moq.Mock<CorvusIAM.Interfaces.ILogAuditoriaService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
                        }, "mock"))
                    }
                }
            };

            var result = await controller.Consultar(1) as OkObjectResult;

            Assert.NotNull(result);
            var item = Assert.IsType<UsuarioResponseDTO>(result.Value);
            Assert.Equal("Test", item.Nome);
        }

        [Fact]
        public async Task Cadastrar_CriaNovoUsuarioERetornaCreatedAtAction()
        {
            var context = GetDbContext();
            var controller = new UsuariosController(context, new Moq.Mock<CorvusIAM.Interfaces.ILogAuditoriaService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
                        }, "mock"))
                    }
                }
            };

            var dto = new UsuarioCreateDTO { Nome = "New User", Email = "new@test.com", Senha = "Password123!" };

            var result = await controller.Cadastrar(dto) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal("Consultar", result.ActionName);
            var item = Assert.IsType<UsuarioResponseDTO>(result.Value);
            Assert.Equal("New User", item.Nome);
        }

        [Fact]
        public async Task Editar_AtualizaDados()
        {
            var context = GetDbContext();
            context.Usuarios.Add(new Usuario { Id = 1, Nome = "Old Name", Email = "old@test.com" });
            await context.SaveChangesAsync();
            var controller = new UsuariosController(context, new Moq.Mock<CorvusIAM.Interfaces.ILogAuditoriaService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
                        }, "mock"))
                    }
                }
            };

            var dto = new UsuarioUpdateDTO { Id = 1, Nome = "New Name", Email = "new@test.com" };
            
            var result = await controller.Editar(1, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = await context.Usuarios.FindAsync(1);
            Assert.Equal("New Name", updated.Nome);
        }
    }
}





