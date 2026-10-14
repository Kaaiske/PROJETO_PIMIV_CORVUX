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

        [Fact]
        public async System.Threading.Tasks.Task TestarAssinaturaIds_DetectaSQLInjectionEBloqueiaNoFirewall()
        {
            var context = GetDbContext();
            var controller = CreateController(context);

            var dto = new AdminController.IdsTestDTO
            {
                Ip = "198.51.100.88",
                Payload = "' OR '1'='1' --"
            };

            var res = await controller.TestarAssinaturaIds(dto) as OkObjectResult;
            Assert.NotNull(res);

            // Verifica se o IP foi adicionado às regras ativas de Firewall
            var regra = context.FirewallRegras.FirstOrDefault(f => f.Ip == "198.51.100.88" && f.Ativo);
            Assert.NotNull(regra);
            Assert.Contains("SQL Injection", regra.Motivo);

            // Verifica se o log de auditoria crítico foi gerado
            var log = context.LogsAuditoria.FirstOrDefault(l => l.Origem == "Corvus IDS Engine" && l.Tipo == "Critico");
            Assert.NotNull(log);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReiniciarServicoApp_RegistraAuditoriaERetornaOnline()
        {
            var context = GetDbContext();
            var controller = CreateController(context);

            var res = await controller.ReiniciarServicoApp() as OkObjectResult;
            Assert.NotNull(res);

            var log = context.LogsAuditoria.FirstOrDefault(l => l.Acao.Contains("Reinício de Serviço Web"));
            Assert.NotNull(log);
        }

        [Fact]
        public void ObterConexoesEndpoint_RetornaConexoesEUsuario()
        {
            var context = GetDbContext();
            context.DispositivosRede.Add(new DispositivoRede
            {
                Hostname = "SRV-TEST-01",
                Ip = "10.0.1.5",
                UsuarioResponsavel = "Admin TI",
                Tipo = "servidores",
                Status = "Saudavel"
            });
            context.SaveChanges();

            var controller = CreateController(context);
            var res = controller.ObterConexoesEndpoint("SRV-TEST-01") as OkObjectResult;
            Assert.NotNull(res);
        }
    }
}

