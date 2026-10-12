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
    public class PoliticasAcessoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PoliticasAcessoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
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

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
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

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
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

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
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
