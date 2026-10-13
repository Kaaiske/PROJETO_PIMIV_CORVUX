using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using CorvusIAM.Controllers;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models;

namespace CorvusIAM.Tests.Controllers
{
    public class PortalContabilidadeRHTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private PortalController CreateController(ApplicationDbContext context)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var controller = new PortalController(
                context,
                new Moq.Mock<CorvusIAM.Interfaces.IPasswordPwnedService>().Object,
                new Moq.Mock<CorvusIAM.Interfaces.ITotpService>().Object,
                new Moq.Mock<CorvusIAM.Interfaces.IStorageService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        [Fact]
        public async Task EmitirNotaFiscal_PersisteNoBancoELograAuditoria()
        {
            var context = GetDbContext();
            var controller = CreateController(context);

            var dto = new PortalController.EmitirNotaFiscalDTO
            {
                TomadorNome = "Empresa Teste SA",
                TomadorCnpj = "11.222.333/0001-44",
                Valor = 15000.00m,
                Descricao = "Serviços de Consultoria IAM"
            };

            var result = await controller.EmitirNotaFiscal(dto);
            Assert.IsType<OkObjectResult>(result);

            var nf = context.NotasFiscais.FirstOrDefault(n => n.TomadorNome == "Empresa Teste SA");
            Assert.NotNull(nf);
            Assert.Equal(15000.00m, nf.Valor);
            Assert.Equal("Autorizada", nf.Status);
            Assert.Contains(context.LogsAuditoria, l => l.Acao == "Emissão de Nota Fiscal");
        }

        [Fact]
        public async Task ExecutarFechamentoMensal_CalculaValoresEPersiste()
        {
            var context = GetDbContext();
            context.NotasFiscais.Add(new NotaFiscal
            {
                Numero = 2001,
                TomadorNome = "Cliente A",
                TomadorCnpj = "00.000.000/0001-00",
                Valor = 100000.00m,
                Status = "Autorizada"
            });
            context.SaveChanges();

            var controller = CreateController(context);
            var result = await controller.ExecutarFechamentoMensal();
            Assert.IsType<OkObjectResult>(result);

            var fechamento = context.FechamentosMensais.FirstOrDefault();
            Assert.NotNull(fechamento);
            Assert.Equal(100000.00m, fechamento.ReceitaBruta);
            Assert.Equal(28000.00m, fechamento.DespesasOperacionais);
            Assert.Equal(72000.00m, fechamento.LucroLiquido);
        }

        [Fact]
        public async Task ProcessarFolhaPagamento_CalculaEncargosEPersiste()
        {
            var context = GetDbContext();
            context.Usuarios.AddRange(
                new Usuario { Id = 10, Nome = "Colab 1", Email = "c1@test.com", Ativo = true, SenhaHash = "x" },
                new Usuario { Id = 11, Nome = "Colab 2", Email = "c2@test.com", Ativo = true, SenhaHash = "x" }
            );
            context.SaveChanges();

            var controller = CreateController(context);
            var result = await controller.ProcessarFolhaPagamento();
            Assert.IsType<OkObjectResult>(result);

            var folha = context.FolhasPagamento.FirstOrDefault();
            Assert.NotNull(folha);
            Assert.Equal(2, folha.TotalColaboradores);
            Assert.True(folha.ValorBrutoTotal > 0);
            Assert.True(folha.ValorLiquidoTotal > 0);
            Assert.Contains(context.LogsAuditoria, l => l.Acao == "Fechamento de Folha de Pagamento");
        }

        [Fact]
        public async Task SolicitarFerias_PersisteRegistroNoBanco()
        {
            var context = GetDbContext();
            var controller = CreateController(context);

            var dto = new PortalController.SolicitarFeriasDTO
            {
                ColaboradorNome = "Amanda Lima",
                DataInicio = DateTime.UtcNow.AddDays(10),
                Dias = 20
            };

            var result = await controller.SolicitarFerias(dto);
            Assert.IsType<OkObjectResult>(result);

            var ferias = context.RegistrosFerias.FirstOrDefault(f => f.ColaboradorNome == "Amanda Lima");
            Assert.NotNull(ferias);
            Assert.Equal(20, ferias.Dias);
            Assert.Equal("Aprovada", ferias.Status);
        }
    }
}

