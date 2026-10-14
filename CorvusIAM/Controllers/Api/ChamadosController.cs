using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CorvusIAM.Data;
using System.Security.Claims;
using CorvusIAM.Entities;
using System;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using CorvusIAM.Models.DTOs;

namespace CorvusIAM.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChamadosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogAuditoriaService _auditoria;

        public ChamadosController(ApplicationDbContext context, ILogAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        [HttpGet]
        public IActionResult GetMeusChamados()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var chamados = _context.ChamadosSuporte
                .Where(c => c.UsuarioId == userId)
                .OrderByDescending(c => c.DataAbertura)
                .Select(c => new ChamadoResponseDTO {
                    Id = c.Id,
                    UsuarioId = c.UsuarioId,
                    Assunto = c.Assunto,
                    Descricao = c.Descricao,
                    Categoria = c.Categoria,
                    Prioridade = c.Prioridade,
                    Status = c.Status,
                    ResponsavelId = c.ResponsavelId,
                    DataAbertura = c.DataAbertura,
                    DataAtualizacao = c.DataAtualizacao,
                    PrazoSla = c.PrazoSla,
                    DataResolucao = c.DataResolucao,
                    DataFechamento = c.DataFechamento,
                    Solucao = c.Solucao
                })
                .ToList();

            return Ok(chamados);
        }

        [HttpPost]
        public async Task<IActionResult> AbrirChamado([FromBody] ChamadoCreateDTO dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Dados da solicitação não fornecidos." });

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? "Acesso" : dto.Categoria.Trim();
            var prioridade = string.IsNullOrWhiteSpace(dto.Prioridade) ? "Normal" : dto.Prioridade.Trim();
            if (prioridade != "Baixa" && prioridade != "Normal" && prioridade != "Alta" && prioridade != "Critica")
                prioridade = "Normal";

            var chamado = new ChamadoSuporte
            {
                UsuarioId = userId,
                Assunto = dto.Assunto,
                Descricao = dto.Descricao,
                Categoria = categoria,
                Prioridade = prioridade,
                Status = "Aberto",
                DataAbertura = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow,
                PrazoSla = DateTime.UtcNow.AddHours(prioridade == "Critica" ? 4 : prioridade == "Alta" ? 8 : 24)
            };

            _context.ChamadosSuporte.Add(chamado);
            await _context.SaveChangesAsync();
            await _auditoria.RegistrarLog(userId, "Abertura de Chamado", $"Chamado #{chamado.Id} aberto.", "Info", "API");

            return Created(string.Empty, new { message = "Chamado aberto com sucesso", id = chamado.Id });
        }
    }
}
