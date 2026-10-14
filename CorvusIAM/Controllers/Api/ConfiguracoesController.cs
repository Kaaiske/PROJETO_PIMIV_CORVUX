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
    /// Gerenciamento de Parâmetros Globais, Chaves de Gateway e Configurações de Sistema.
    /// </summary>
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("Configurações do Sistema & Gateway")]
    public class ConfiguracoesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista todas as chaves e valores de configuração operacional do sistema (incluindo tokens de gateway e políticas de retenção).
        /// </summary>
        /// <response code="200">Lista consolidada de configurações globais.</response>
        /// <response code="401">Usuário não autenticado.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<ConfiguracaoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Listar()
        {
            var configuracoes = await _context.ConfiguracoesSistema
                .Select(c => new ConfiguracaoResponseDTO
                {
                    Id = c.Id,
                    Chave = c.Chave,
                    Valor = c.Valor
                })
                .ToListAsync();
            return Ok(configuracoes);
        }

        /// <summary>
        /// Obtém o valor de um parâmetro de configuração específico pelo ID.
        /// </summary>
        /// <param name="id">ID numérico da configuração.</param>
        /// <response code="200">Parâmetro de configuração localizado.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="404">Configuração não encontrada.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConfiguracaoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Consultar(int id)
        {
            var configuracao = await _context.ConfiguracoesSistema.FindAsync(id);
            if (configuracao == null) return NotFound();
            
            var response = new ConfiguracaoResponseDTO
            {
                Id = configuracao.Id,
                Chave = configuracao.Chave,
                Valor = configuracao.Valor
            };
            return Ok(response);
        }

        /// <summary>
        /// Cadastra um novo parâmetro de configuração global no repositório.
        /// </summary>
        /// <param name="dto">Chave e valor da nova configuração.</param>
        /// <response code="201">Configuração criada com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'RequireAdmin').</response>
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(ConfiguracaoResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Cadastrar([FromBody] ConfiguracaoCreateDTO dto)
        {
            var configuracao = new ConfiguracaoSistema
            {
                Chave = dto.Chave,
                Valor = dto.Valor
            };
            _context.ConfiguracoesSistema.Add(configuracao);
            await _context.SaveChangesAsync();
            
            var response = new ConfiguracaoResponseDTO
            {
                Id = configuracao.Id,
                Chave = configuracao.Chave,
                Valor = configuracao.Valor
            };
            return CreatedAtAction(nameof(Consultar), new { id = configuracao.Id }, response);
        }

        /// <summary>
        /// Modifica o valor de uma chave de configuração existente.
        /// </summary>
        /// <param name="id">ID da configuração a ser alterada.</param>
        /// <param name="dto">Nova chave e valor atualizado.</param>
        /// <response code="204">Configuração atualizada com sucesso.</response>
        /// <response code="400">ID da rota divergente do corpo.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'RequireAdmin').</response>
        /// <response code="404">Configuração não encontrada.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Editar(int id, [FromBody] ConfiguracaoUpdateDTO dto)
        {
            if (id != dto.Id) return BadRequest();
            
            var configuracao = await _context.ConfiguracoesSistema.FindAsync(id);
            if (configuracao == null) return NotFound();
            
            configuracao.Chave = dto.Chave;
            configuracao.Valor = dto.Valor;
            
            _context.Entry(configuracao).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Remove uma configuração global da base corporativa.
        /// </summary>
        /// <param name="id">ID da configuração a ser removida.</param>
        /// <response code="204">Configuração removida com sucesso.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'RequireAdmin').</response>
        /// <response code="404">Configuração não encontrada.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var configuracao = await _context.ConfiguracoesSistema.FindAsync(id);
            if (configuracao == null) return NotFound();
            _context.ConfiguracoesSistema.Remove(configuracao);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
