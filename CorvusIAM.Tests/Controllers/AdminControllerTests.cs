using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Controllers;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;

namespace CorvusIAM.Tests.Controllers
{
    public class AdminControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options); context.Perfis.Add(new Perfil { Id = 5, Nome = "Colaborador" }); context.SaveChanges(); return context;
        }

        private AdminController CreateControllerWithUser(ApplicationDbContext context)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var controller = new AdminController(context, new Moq.Mock<CorvusIAM.Interfaces.IPasswordPwnedService>().Object, new Moq.Mock<CorvusIAM.Interfaces.IEmailValidationService>().Object, new Moq.Mock<CorvusIAM.Interfaces.IEmailSenderService>().Object, new Moq.Mock<CorvusIAM.Interfaces.IPasswordGeneratorService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        [Fact]
        public void Index_RetornaViewComDashboardViewModel()
        {
            var context = GetDbContext();
            var controller = CreateControllerWithUser(context);

            var result = controller.Index() as ViewResult;

            Assert.NotNull(result);
            Assert.IsType<DashboardViewModel>(result.Model);
        }

        [Fact]
        public void AdicionarPolitica_AdicionaE_RetornaOk()
        {
            var context = GetDbContext();
            var controller = CreateControllerWithUser(context);
            var politica = new PoliticaAcesso { Nome = "Nova Politica", Descricao = "Regra 1" };

            var result = controller.AdicionarPolitica(politica);

            Assert.IsType<OkResult>(result);
            Assert.Single(context.PoliticasAcesso);
            Assert.True(context.PoliticasAcesso.First().Ativa);
        }

        [Fact]
        public void InativarUsuario_InverteStatusDoUsuario()
        {
            var context = GetDbContext();
            var controller = CreateControllerWithUser(context);
            var usuario = new Usuario { Id = 2, Nome = "Test", Email = "test@test.com", Ativo = true };
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var result = controller.InativarUsuario(2);

            Assert.IsType<OkResult>(result);
            var updatedUser = context.Usuarios.Find(2);
            Assert.False(updatedUser.Ativo);
        }

        [Fact]
        public void ExcluirUsuario_RemoveUsuarioERegraLog()
        {
            var context = GetDbContext();
            var controller = CreateControllerWithUser(context);
            var usuario = new Usuario { Id = 2, Nome = "Test", Email = "test@test.com" };
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var result = controller.ExcluirUsuario(2);

            Assert.IsType<OkResult>(result);
            var deletedUser = context.Usuarios.Find(2); Assert.False(deletedUser.Ativo); Assert.Equal("Usuário Excluído", deletedUser.Nome);
            Assert.Contains(context.LogsAuditoria, l => l.Acao == "Exclusão de Colaborador");
        }
    }
}





