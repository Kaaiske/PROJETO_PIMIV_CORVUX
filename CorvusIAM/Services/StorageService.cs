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
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // Limite rigoroso de 5MB
        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".pdf", ".txt", ".csv", ".log" };

        public StorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
            
            // Keep attachments outside wwwroot so they are never publicly downloadable
            // without the authorization check in AnexosController.
            var contentRoot = _environment.ContentRootPath
                ?? _environment.WebRootPath
                ?? Directory.GetCurrentDirectory();
            _uploadDirectory = Path.Combine(contentRoot, "uploads", "chamados");
            
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("O fluxo do arquivo está vazio.");

            if (stream.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"O arquivo excede o limite máximo permitido de 5 MB ({stream.Length / (1024 * 1024)} MB enviado).");

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"Extensão de arquivo não permitida. Permitidas: {string.Join(", ", AllowedExtensions)}");
            }

            // Inspeção de Magic Bytes para bloquear executáveis binários (PE / MZ / ELF) e scripts disfarçados
            if (stream.CanSeek)
            {
                stream.Position = 0;
                var headerBytes = new byte[4];
                int read = await stream.ReadAsync(headerBytes, 0, headerBytes.Length);
                stream.Position = 0;

                if (read >= 2)
                {
                    // Bloqueio de executáveis Windows (MZ / PE)
                    if (headerBytes[0] == 0x4D && headerBytes[1] == 0x5A)
                        throw new InvalidOperationException("Arquivos executáveis binários (MZ/PE) são estritamente proibidos.");

                    // Bloqueio de binários Linux (ELF)
                    if (read >= 4 && headerBytes[0] == 0x7F && headerBytes[1] == 0x45 && headerBytes[2] == 0x4C && headerBytes[3] == 0x46)
                        throw new InvalidOperationException("Arquivos binários executáveis (ELF) são estritamente proibidos.");

                    // Bloqueio de webshells PHP
                    if (read >= 4 && headerBytes[0] == 0x3C && headerBytes[1] == 0x3F && headerBytes[2] == 0x70 && headerBytes[3] == 0x68) // <?ph
                        throw new InvalidOperationException("Scripts executáveis são estritamente proibidos.");
                }

                var isValidSignature = extension switch
                {
                    ".png" => read == 4 && headerBytes.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
                    ".jpg" or ".jpeg" => read >= 2 && headerBytes[0] == 0xFF && headerBytes[1] == 0xD8,
                    ".pdf" => read >= 4 && headerBytes[0] == 0x25 && headerBytes[1] == 0x50 && headerBytes[2] == 0x44 && headerBytes[3] == 0x46,
                    _ => true
                };

                if (!isValidSignature)
                    throw new InvalidOperationException("A assinatura do conteúdo não corresponde à extensão informada.");
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

            return safeFileName;
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
            string cleanKey = Path.GetFileName(fileKey);
            return $"/api/chamados/anexos/{Uri.EscapeDataString(cleanKey)}";
        }

        public Task<bool> DeleteFileAsync(string fileKey)
        {
            string cleanKey = Path.GetFileName(fileKey);
            string fullPath = Path.Combine(_uploadDirectory, cleanKey);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    return Task.FromResult(true);
                }
                catch
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(false);
        }
    }
}
