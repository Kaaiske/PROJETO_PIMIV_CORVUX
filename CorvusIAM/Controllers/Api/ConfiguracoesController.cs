using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Models.DTOs;

namespace CorvusIAM.Controllers.Api
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/[controller]")]
    [Authorize]
    public class ConfiguracoesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
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

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
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

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
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

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
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
