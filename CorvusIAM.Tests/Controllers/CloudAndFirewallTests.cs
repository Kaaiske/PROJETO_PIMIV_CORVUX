using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Controllers;
using CorvusIAM.Data;
using CorvusIAM.Entities;

namespace CorvusIAM.Tests.Controllers
{
    public class CloudAndFirewallTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            return context;
        }

        private AdminController CreateController(ApplicationDbContext context)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var controller = new AdminController(
                context,
                new Moq.Mock<CorvusIAM.Interfaces.IPasswordPwnedService>().Object,
                new Moq.Mock<CorvusIAM.Interfaces.IEmailValidationService>().Object,
                new Moq.Mock<CorvusIAM.Interfaces.IEmailSenderService>().Object,
                new Moq.Mock<CorvusIAM.Interfaces.IPasswordGeneratorService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        [Fact]
        public void Cloud_RetornaViewComDispositivosDoBanco()
        {
            var context = GetDbContext();
            context.DispositivosRede.Add(new DispositivoRede
            {
                Hostname = "WS-TESTE-99",
                Ip = "192.168.1.99",
                Tipo = "estacoes",
                Status = "Saudavel"
            });
            context.SaveChanges();

            var controller = CreateController(context);
            var result = controller.Cloud() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<IEnumerable<DispositivoRede>>(result.Model);
            Assert.Contains(model, d => d.Hostname == "WS-TESTE-99");
        }

        [Fact]
        public void GerenciarFirewallIp_BloqueiaEDesbloqueiaCorretamente()
        {
            var context = GetDbContext();
            var controller = CreateController(context);

            var dtoBloqueio = new AdminController.FirewallIpDTO
            {
                Ip = "200.100.50.25",
                Motivo = "Tentativa de Exploit",
                Acao = "Bloquear"
            };

            var resBloqueio = controller.GerenciarFirewallIp(dtoBloqueio);
            Assert.IsType<OkObjectResult>(resBloqueio);
            Assert.True(context.FirewallRegras.Any(f => f.Ip == "200.100.50.25" && f.Ativo));

            var dtoDesbloqueio = new AdminController.FirewallIpDTO
            {
                Ip = "200.100.50.25",
                Acao = "Desbloquear"
            };

            var resDesbloqueio = controller.GerenciarFirewallIp(dtoDesbloqueio);
            Assert.IsType<OkObjectResult>(resDesbloqueio);
            Assert.False(context.FirewallRegras.Any(f => f.Ip == "200.100.50.25" && f.Ativo));
        }

        [Fact]
        public void ExecutarAcaoEndpoint_BatchRemediate_NormalizaDispositivos()
        {
            var context = GetDbContext();
            context.DispositivosRede.Add(new DispositivoRede
            {
                Hostname = "WS-ALERT-01",
                Ip = "192.168.10.99",
                Tipo = "estacoes",
                Status = "Alerta",
                PatchesPendentes = 4
            });
            context.SaveChanges();

            var controller = CreateController(context);
            var res = controller.ExecutarAcaoEndpoint(new AdminController.EndpointAcaoDTO
            {
                Acao = "BatchRemediate"
            });

            Assert.IsType<OkObjectResult>(res);
            var dev = context.DispositivosRede.First(d => d.Hostname == "WS-ALERT-01");
            Assert.Equal("Saudavel", dev.Status);
            Assert.Equal(0, dev.PatchesPendentes);
        }
    }
}

