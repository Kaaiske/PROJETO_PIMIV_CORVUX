using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/chamados/anexos")]
    public class AnexosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storage;

        public AnexosController(ApplicationDbContext context, IStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        [HttpGet("{fileKey}")]
        public async Task<IActionResult> Download(string fileKey)
        {
            var safeKey = Path.GetFileName(fileKey);
            var url = _storage.GetFileUrl(safeKey);
            var chamado = await _context.ChamadosSuporte
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AnexoUrl == url);

            if (chamado == null)
                return NotFound();

            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
                ? parsedId
                : 0;
            var allowed = chamado.UsuarioId == userId || User.IsInRole("Admin") || User.IsInRole("Help Desk");
            if (!allowed)
                return Forbid();

            var stream = await _storage.DownloadFileAsync(safeKey);
            if (stream == null)
                return NotFound();

            var contentType = Path.GetExtension(safeKey).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".csv" => "text/csv",
                _ => "text/plain"
            };

            return File(stream, contentType, chamado.AnexoNomeOriginal ?? safeKey);
        }
    }
}
