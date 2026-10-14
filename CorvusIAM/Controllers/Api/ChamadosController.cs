using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CorvusIAM.Data;
using System.Security.Claims;
using CorvusIAM.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using CorvusIAM.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Service Desk Corporativo: Abertura, acompanhamento e resolução de chamados de suporte técnico e requisições de acesso.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Service Desk & Chamados")]
    public class ChamadosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogAuditoriaService _auditoria;

        public ChamadosController(ApplicationDbContext context, ILogAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        /// <summary>
        /// Obtém a listagem completa de chamados abertos pelo colaborador autenticado.
        /// </summary>
        /// <response code="200">Lista de chamados retornada com sucesso.</response>
        /// <response code="401">Usuário não autenticado.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<ChamadoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Consulta os detalhes de um chamado específico pelo seu identificador.
        /// </summary>
        /// <param name="id">Identificador numérico do chamado.</param>
        /// <response code="200">Detalhes do chamado retornados.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="404">Chamado não encontrado ou pertencente a outro usuário.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ChamadoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChamadoById(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var chamado = await _context.ChamadosSuporte
                .AsNoTracking()
                .Where(c => c.Id == id && (c.UsuarioId == userId || User.IsInRole("Admin") || User.IsInRole("TI_Suporte")))
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
                .FirstOrDefaultAsync();

            if (chamado == null) return NotFound(new { message = "Chamado não encontrado." });

            return Ok(chamado);
        }

        /// <summary>
        /// Abre um novo chamado técnico ou requisição de permissão no Service Desk corporativo.
        /// </summary>
        /// <remarks>
        /// O prazo de SLA é calculado dinamicamente de acordo com a prioridade selecionada:
        /// - **Crítica**: SLA de 4 horas
        /// - **Alta**: SLA de 8 horas
        /// - **Normal / Baixa**: SLA de 24 horas
        /// </remarks>
        /// <param name="dto">Dados do chamado (Assunto, Descrição, Categoria, Prioridade).</param>
        /// <response code="201">Chamado criado com sucesso com ID gerado.</response>
        /// <response code="400">Dados da requisição inválidos.</response>
        /// <response code="401">Usuário não autenticado.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
