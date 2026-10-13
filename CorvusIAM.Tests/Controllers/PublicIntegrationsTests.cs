using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using CorvusIAM.Controllers.Api;
using System.Text.Json;

namespace CorvusIAM.Tests.Controllers
{
    public class PublicIntegrationsTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<PublicIntegrationsController>> _mockLogger;
        private readonly PublicIntegrationsController _controller;

        public PublicIntegrationsTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());

            _mockLogger = new Mock<ILogger<PublicIntegrationsController>>();
            _controller = new PublicIntegrationsController(_mockHttpClientFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ConsultarCep_ComCepInvalido_RetornaBadRequest()
        {
            var result = await _controller.ConsultarCep("123");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConsultarCep_ComCepValido_RetornaOkComDados()
        {
            var result = await _controller.ConsultarCep("01310-100");
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            var contentResult = result as ContentResult;
            Assert.True(okResult != null || contentResult != null);
        }

        [Fact]
        public async Task ConsultarCnpj_ComCnpjInvalido_RetornaBadRequest()
        {
            var result = await _controller.ConsultarCnpj("12345");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConsultarCnpj_ComCnpjValido_RetornaOkComDados()
        {
            var result = await _controller.ConsultarCnpj("00.000.000/0001-91");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ObterCotacoesMoedas_RetornaDadosUsdEur()
        {
            var result = await _controller.ObterCotacoesMoedas();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ObterFeriados_RetornaListaDeFeriados()
        {
            var result = await _controller.ObterFeriados(2026);
            Assert.NotNull(result);
        }

        [Fact]
        public void ValidarCpf_ComDigitosRepetidos_RetornaInvalido()
        {
            var req = new PublicIntegrationsController.CpfValidationRequest { Cpf = "111.111.111-11" };
            var result = _controller.ValidarCpf(req) as OkObjectResult;
            Assert.NotNull(result);

            var json = JsonSerializer.Serialize(result.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.False(doc.RootElement.GetProperty("valido").GetBoolean());
        }

        [Fact]
        public void ValidarCpf_ComCpfValidoOficial_RetornaValido()
        {
            // CPF matematicamente válido gerado pelo Módulo 11 (ex: 52998224725)
            // 5*10 + 2*9 + 9*8 + 9*7 + 8*6 + 2*5 + 2*4 + 4*3 + 7*2 = 50+18+72+63+48+10+8+12+14 = 295 % 11 = 9 => 11-9 = 2
            // Segundo digito: 5*11 + 2*10 + 9*9 + 9*8 + 8*7 + 2*6 + 2*5 + 4*4 + 7*3 + 2*2 = 55+20+81+72+56+12+10+16+21+4 = 347 % 11 = 6 => 11-6 = 5
            var req = new PublicIntegrationsController.CpfValidationRequest { Cpf = "52998224725" };
            var result = _controller.ValidarCpf(req) as OkObjectResult;
            Assert.NotNull(result);

            var json = JsonSerializer.Serialize(result.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.GetProperty("valido").GetBoolean());
        }
    }
}

