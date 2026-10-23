using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Políticas Corporativas de Segurança e Acesso Condicional (ABAC - Attribute-Based Access Control).
    /// </summary>
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireJwt")]
    [Tags("Políticas de Segurança & Regras (ABAC)")]
    public class PoliticasAcessoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PoliticasAcessoController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista todas as políticas de segurança e regras de acesso condicional vigentes na organização.
        /// </summary>
        /// <response code="200">Lista consolidada de políticas ABAC.</response>
        /// <response code="401">Usuário não autenticado.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<PoliticaResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Listar()
        {
            var PoliticasAcesso = await _context.PoliticasAcesso
                .Select(p => new PoliticaResponseDTO
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Ativa = p.Ativa
                })
                .ToListAsync();
            return Ok(PoliticasAcesso);
        }

        /// <summary>
        /// Consulta uma política de segurança específica pelo identificador.
        /// </summary>
        /// <param name="id">ID numérico da política de segurança.</param>
        /// <response code="200">Detalhes da política retornados.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="404">Política não localizada na base de dados.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PoliticaResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Consultar(int id)
        {
            var p = await _context.PoliticasAcesso.FindAsync(id);
            if (p == null) return NotFound();
            
            var response = new PoliticaResponseDTO
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                Ativa = p.Ativa
            };
            return Ok(response);
        }

        /// <summary>
        /// Cadastra uma nova política de acesso condicional ou conformidade.
        /// </summary>
        /// <param name="dto">Dados da política (Nome, Descrição e se está Ativa).</param>
        /// <response code="201">Política cadastrada com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'RequireAdmin').</response>
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(PoliticaResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Cadastrar([FromBody] PoliticaCreateDTO dto)
        {
            var politica = new PoliticaAcesso
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                Ativa = dto.Ativa
            };
            _context.PoliticasAcesso.Add(politica);
            await _context.SaveChangesAsync();
            
            var response = new PoliticaResponseDTO
            {
                Id = politica.Id,
                Nome = politica.Nome,
                Descricao = politica.Descricao,
                Ativa = politica.Ativa
            };
            return CreatedAtAction(nameof(Consultar), new { id = politica.Id }, response);
        }

        /// <summary>
        /// Atualiza as configurações de uma política de segurança existente.
        /// </summary>
        /// <param name="id">ID da política a ser atualizada.</param>
        /// <param name="dto">Novas configurações da política.</param>
        /// <response code="204">Política atualizada com sucesso.</response>
        /// <response code="400">ID da rota divergente do corpo.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'RequireAdmin').</response>
        /// <response code="404">Política não encontrada.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Editar(int id, [FromBody] PoliticaUpdateDTO dto)
        {
            if (id != dto.Id) return BadRequest();
            
            var politica = await _context.PoliticasAcesso.FindAsync(id);
            if (politica == null) return NotFound();
            
            politica.Nome = dto.Nome;
            politica.Descricao = dto.Descricao;
            politica.Ativa = dto.Ativa;

            _context.Entry(politica).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Exclui uma política de segurança do catálogo corporativo.
        /// </summary>
        /// <param name="id">ID da política a ser excluída.</param>
        /// <response code="204">Política excluída com sucesso.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'RequireAdmin').</response>
        /// <response code="404">Política não encontrada.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var politica = await _context.PoliticasAcesso.FindAsync(id);
            if (politica == null) return NotFound();
            _context.PoliticasAcesso.Remove(politica);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
