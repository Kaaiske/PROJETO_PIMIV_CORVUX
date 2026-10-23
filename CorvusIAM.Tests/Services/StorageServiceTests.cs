using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CorvusIAM.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CorvusIAM.Tests.Services
{
    public class StorageServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly StorageService _storageService;

        public StorageServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "CorvusIAM_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(_tempDir);
            mockEnv.Setup(e => e.ContentRootPath).Returns(_tempDir);

            var mockConfig = new Mock<IConfiguration>();

            _storageService = new StorageService(mockEnv.Object, mockConfig.Object);
        }

        [Fact]
        public async Task UploadFileAsync_SalvaArquivoValido_ComSucesso()
        {
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\nRelatorio de Auditoria");
            using var stream = new MemoryStream(bytes);

            string fileKey = await _storageService.UploadFileAsync(stream, "teste.pdf", "application/pdf");

            Assert.NotNull(fileKey);
            Assert.EndsWith(".pdf", fileKey);
            Assert.StartsWith("/api/chamados/anexos/", _storageService.GetFileUrl(fileKey));
        }

        [Fact]
        public async Task UploadFileAsync_LancaExcecao_ParaExtensaoProibida()
        {
            var bytes = Encoding.UTF8.GetBytes("echo malicioso");
            using var stream = new MemoryStream(bytes);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _storageService.UploadFileAsync(stream, "script.exe", "application/octet-stream"));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }
    }
}

