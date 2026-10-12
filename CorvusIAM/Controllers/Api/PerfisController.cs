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
    public class PerfisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PerfisController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
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

        [HttpPost]
        [Authorize(Policy = "CanManageProfiles")]
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

        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageProfiles")]
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

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanManageProfiles")]
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
