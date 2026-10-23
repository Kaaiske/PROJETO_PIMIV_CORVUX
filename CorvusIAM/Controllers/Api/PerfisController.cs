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
    /// Governança de Perfis e Papéis de Acesso (RBAC - Role-Based Access Control).
    /// </summary>
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireJwt")]
    [Tags("Perfis & Papéis de Acesso (RBAC)")]
    public class PerfisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PerfisController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista todos os perfis corporativos cadastrados (ex: Administrador, TI Suporte, Financeiro, RH, Operacional).
        /// </summary>
        /// <response code="200">Lista consolidada de perfis RBAC.</response>
        /// <response code="401">Usuário não autenticado.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<PerfilResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Listar()
        {
            var perfis = await _context.Perfis
                .Select(p => new PerfilResponseDTO
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao
                })
                .ToListAsync();
            return Ok(perfis);
        }

        /// <summary>
        /// Consulta um perfil específico pelo seu ID.
        /// </summary>
        /// <param name="id">ID numérico do perfil.</param>
        /// <response code="200">Perfil localizado com sucesso.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="404">Perfil não encontrado na base de dados.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PerfilResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Consultar(int id)
        {
            var perfil = await _context.Perfis.FindAsync(id);
            if (perfil == null) return NotFound();
            
            var response = new PerfilResponseDTO
            {
                Id = perfil.Id,
                Nome = perfil.Nome,
                Descricao = perfil.Descricao
            };
            return Ok(response);
        }

        /// <summary>
        /// Cria um novo papel ou perfil de acesso no catálogo corporativo.
        /// </summary>
        /// <param name="dto">Dados do perfil (Nome e Descrição de atribuições).</param>
        /// <response code="201">Perfil criado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'CanManageProfiles').</response>
        [HttpPost]
        [Authorize(Policy = "CanManageProfiles")]
        [ProducesResponseType(typeof(PerfilResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Cadastrar([FromBody] PerfilCreateDTO dto)
        {
            var perfil = new Perfil
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao
            };
            _context.Perfis.Add(perfil);
            await _context.SaveChangesAsync();
            
            var response = new PerfilResponseDTO
            {
                Id = perfil.Id,
                Nome = perfil.Nome,
                Descricao = perfil.Descricao
            };
            return CreatedAtAction(nameof(Consultar), new { id = perfil.Id }, response);
        }

        /// <summary>
        /// Atualiza o nome ou a descrição de um perfil de acesso existente.
        /// </summary>
        /// <param name="id">ID do perfil a ser modificado.</param>
        /// <param name="dto">Novos dados do perfil.</param>
        /// <response code="204">Perfil atualizado com sucesso.</response>
        /// <response code="400">ID da rota divergente do corpo.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'CanManageProfiles').</response>
        /// <response code="404">Perfil não encontrado.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageProfiles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Editar(int id, [FromBody] PerfilUpdateDTO dto)
        {
            if (id != dto.Id) return BadRequest();
            
            var perfil = await _context.Perfis.FindAsync(id);
            if (perfil == null) return NotFound();
            
            perfil.Nome = dto.Nome;
            perfil.Descricao = dto.Descricao;
            
            _context.Entry(perfil).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Remove um perfil corporativo da base de dados.
        /// </summary>
        /// <param name="id">ID do perfil a ser excluído.</param>
        /// <response code="204">Perfil excluído com sucesso.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'CanManageProfiles').</response>
        /// <response code="404">Perfil não encontrado.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanManageProfiles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var perfil = await _context.Perfis.FindAsync(id);
            if (perfil == null) return NotFound();
            _context.Perfis.Remove(perfil);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
