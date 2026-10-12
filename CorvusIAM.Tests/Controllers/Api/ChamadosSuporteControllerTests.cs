using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Controllers.Api;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using Moq;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Tests.Controllers.Api
{
    public class ChamadosSuporteControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private ChamadosController CreateControllerWithUser(ApplicationDbContext context, int userId, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            var mockAuditoria = new Mock<ILogAuditoriaService>();

            var controller = new ChamadosController(context, mockAuditoria.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        [Fact]
        public void GetMeusChamados_UsuarioComumVeApenasOsSeusChamados()
        {
            var context = GetDbContext();
            context.ChamadosSuporte.Add(new ChamadoSuporte { Id = 1, UsuarioId = 1, Assunto = "Issue 1" });
            context.ChamadosSuporte.Add(new ChamadoSuporte { Id = 2, UsuarioId = 2, Assunto = "Issue 2" });
            context.SaveChanges();

            var controller = CreateControllerWithUser(context, 1, "User");

            var result = controller.GetMeusChamados() as OkObjectResult;
            Assert.NotNull(result);
            var items = Assert.IsType<List<ChamadoResponseDTO>>(result.Value);
            Assert.Single(items);
            Assert.Equal(1, items.First().UsuarioId);
        }

        [Fact]
        public async Task AbrirChamado_CriaChamadoComIdDoUsuario()
        {
            var context = GetDbContext();
            var controller = CreateControllerWithUser(context, 5, "User");

            var dto = new ChamadoCreateDTO { Assunto = "New Issue", Descricao = "Details", Categoria = "Suporte", Prioridade = "Alta" };
            var result = await controller.AbrirChamado(dto) as CreatedResult;

            Assert.NotNull(result);
            var contextItem = context.ChamadosSuporte.FirstOrDefault(c => c.Assunto == "New Issue");
            Assert.NotNull(contextItem);
            Assert.Equal(5, contextItem.UsuarioId);
            Assert.Equal("New Issue", contextItem.Assunto);
        }
    }
}
