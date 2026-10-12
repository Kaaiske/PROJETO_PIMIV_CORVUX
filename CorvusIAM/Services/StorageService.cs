using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CorvusIAM.Services
{
    public class StorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly string _uploadDirectory;
        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".pdf", ".txt", ".csv", ".log" };

        public StorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
            
            string webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _uploadDirectory = Path.Combine(webRoot, "uploads", "chamados");
            
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("O fluxo do arquivo está vazio.");

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"Extensão de arquivo não permitida. Permitidas: {string.Join(", ", AllowedExtensions)}");
            }

            // Sanitiza e gera nome único baseado em GUID
            string safeFileName = $"{Guid.NewGuid():N}{extension}";
            string destinationPath = Path.Combine(_uploadDirectory, safeFileName);

            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                await stream.CopyToAsync(fileStream);
            }

            return $"/uploads/chamados/{safeFileName}";
        }

        public Task<Stream?> DownloadFileAsync(string fileKey)
        {
            string cleanKey = Path.GetFileName(fileKey);
            string fullPath = Path.Combine(_uploadDirectory, cleanKey);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream?>(stream);
        }

        public string GetFileUrl(string fileKey)
        {
            if (string.IsNullOrWhiteSpace(fileKey)) return string.Empty;
            if (fileKey.StartsWith("http://") || fileKey.StartsWith("https://")) return fileKey;
            return fileKey.StartsWith("/") ? fileKey : $"/{fileKey}";
        }
    }
}
