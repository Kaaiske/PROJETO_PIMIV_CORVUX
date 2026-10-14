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
using System.Security.Claims;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CorvusIAM.Controllers.Api
{
    /// <summary>
    /// Gerenciamento Centralizado de Usuários Corporativos (IAM Core): Criação, edição, perfis de acesso e anonimização em conformidade com a LGPD.
    /// </summary>
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("Gestão de Usuários (IAM Core)")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogAuditoriaService _auditoria;

        public UsuariosController(ApplicationDbContext context, ILogAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        /// <summary>
        /// Lista todos os colaboradores cadastrados na base corporativa.
        /// </summary>
        /// <response code="200">Lista consolidada de colaboradores retornada com sucesso.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'CanViewUsers').</response>
        [HttpGet]
        [Authorize(Policy = "CanViewUsers")]
        [ProducesResponseType(typeof(List<UsuarioResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Listar()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioResponseDTO
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Ativo = u.Ativo,
                    CPF = u.CPF,
                    DataNascimento = u.DataNascimento,
                    Cargo = u.Cargo,
                    DataContratacao = u.DataContratacao,
                    RG = u.RG,
                    Telefone = u.Telefone,
                    Departamento = u.Departamento,
                    Endereco = u.Endereco
                })
                .ToListAsync();
            
            return Ok(usuarios);
        }

        /// <summary>
        /// Obtém o perfil e as informações cadastrais do usuário atualmente conectado na sessão.
        /// </summary>
        /// <response code="200">Dados do perfil do usuário autenticado.</response>
        /// <response code="401">Usuário não autenticado.</response>
        /// <response code="404">Registro do usuário não encontrado na base.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MeuPerfil()
        {
            var usuarioId = UsuarioAtualId();
            if (usuarioId <= 0) return Unauthorized();

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null) return NotFound();

            var perfilId = usuario.UsuarioPerfis.Select(up => (int?)up.PerfilId).FirstOrDefault();
            return Ok(new UsuarioResponseDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Ativo = usuario.Ativo,
                CPF = usuario.CPF,
                DataNascimento = usuario.DataNascimento,
                Cargo = usuario.Cargo,
                DataContratacao = usuario.DataContratacao,
                RG = usuario.RG,
                Telefone = usuario.Telefone,
                Departamento = usuario.Departamento,
                Endereco = usuario.Endereco,
                PerfilId = perfilId
            });
        }

        /// <summary>
        /// Consulta um colaborador específico através do seu ID identificador.
        /// </summary>
        /// <param name="id">ID numérico do usuário.</param>
        /// <response code="200">Colaborador localizado com sucesso.</response>
        /// <response code="401">Não autenticado.</response>
        /// <response code="403">Permissão insuficiente (requer 'CanViewUsers').</response>
        /// <response code="404">Colaborador não encontrado.</response>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanViewUsers")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Consultar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            
            var response = new UsuarioResponseDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Ativo = usuario.Ativo,
                CPF = usuario.CPF,
                DataNascimento = usuario.DataNascimento,
                Cargo = usuario.Cargo,
                DataContratacao = usuario.DataContratacao,
                RG = usuario.RG,
                Telefone = usuario.Telefone,
                Departamento = usuario.Departamento,
                Endereco = usuario.Endereco
            };
            return Ok(response);
        }

        /// <summary>
        /// Cadastra um novo colaborador no diretório corporativo com hashing seguro de senha (BCrypt) e atribuição de perfil RBAC.
        /// </summary>
        /// <param name="dto">Dados de cadastro do novo colaborador.</param>
        /// <response code="201">Colaborador criado com sucesso.</response>
        /// <response code="400">Dados inválidos ou requisitos de complexidade de senha não atendidos.</response>
        /// <response code="409">Conflito: E-mail ou CPF já pertencem a outro colaborador cadastrado.</response>
        [HttpPost]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cadastrar([FromBody] UsuarioCreateDTO dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email || (!string.IsNullOrWhiteSpace(dto.CPF) && u.CPF == dto.CPF)))
                return Conflict(new { message = "E-mail ou CPF já cadastrado." });
            if (!SenhaValida(dto.Senha))
                return BadRequest(new { message = "A senha deve conter 8 caracteres, maiúscula, minúscula, número e símbolo." });

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                Ativo = dto.Ativo,
                CPF = dto.CPF,
                DataNascimento = dto.DataNascimento,
                Cargo = dto.Cargo,
                DataContratacao = dto.DataContratacao,
                RG = dto.RG,
                Telefone = dto.Telefone,
                Departamento = dto.Departamento,
                Endereco = dto.Endereco
            };
            
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            var perfilId = dto.PerfilId ?? 5;
            if (!await _context.Perfis.AnyAsync(p => p.Id == perfilId)) return BadRequest(new { message = "Perfil inválido." });
            _context.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = usuario.Id, PerfilId = perfilId });
            await _context.SaveChangesAsync();
            await _auditoria.RegistrarLog(UsuarioAtualId(), "Criação de Usuário", $"Usuário {usuario.Email} criado.", "Info", "API");
            
            var response = new UsuarioResponseDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Ativo = usuario.Ativo,
                CPF = usuario.CPF,
                DataNascimento = usuario.DataNascimento,
                Cargo = usuario.Cargo,
                DataContratacao = usuario.DataContratacao,
                RG = usuario.RG,
                Telefone = usuario.Telefone,
                Departamento = usuario.Departamento,
                Endereco = usuario.Endereco
            };
            
            return CreatedAtAction(nameof(Consultar), new { id = usuario.Id }, response);
        }

        /// <summary>
        /// Atualiza os dados cadastrais, cargo, status (ativo/bloqueado) ou senha de um colaborador existente.
        /// </summary>
        /// <param name="id">ID do colaborador a atualizar.</param>
        /// <param name="dto">Novos dados cadastrais.</param>
        /// <response code="204">Colaborador atualizado com sucesso.</response>
        /// <response code="400">ID da rota divergente do corpo ou payload inválido.</response>
        /// <response code="404">Colaborador não encontrado.</response>
        /// <response code="409">Conflito com e-mail ou CPF de outro colaborador.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Editar(int id, [FromBody] UsuarioUpdateDTO dto)
        {
            if (id != dto.Id) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            if (await _context.Usuarios.AnyAsync(u => u.Id != id && (u.Email == dto.Email || (!string.IsNullOrWhiteSpace(dto.CPF) && u.CPF == dto.CPF))))
                return Conflict(new { message = "E-mail ou CPF já cadastrado." });
            if (!string.IsNullOrEmpty(dto.Senha) && !SenhaValida(dto.Senha))
                return BadRequest(new { message = "A senha deve conter 8 caracteres, maiúscula, minúscula, número e símbolo." });
            
            usuario.Nome = dto.Nome;
            usuario.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Senha))
                usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
            usuario.Ativo = dto.Ativo;
            usuario.CPF = dto.CPF;
            usuario.DataNascimento = dto.DataNascimento;
            usuario.Cargo = dto.Cargo;
            usuario.DataContratacao = dto.DataContratacao;
            usuario.RG = dto.RG;
            usuario.Telefone = dto.Telefone;
            usuario.Departamento = dto.Departamento;
            usuario.Endereco = dto.Endereco;

            if (dto.PerfilId.HasValue)
            {
                var perfil = await _context.Perfis.FindAsync(dto.PerfilId.Value);
                if (perfil == null) return BadRequest(new { message = "Perfil inválido." });
                var vinculos = await _context.UsuarioPerfis.Where(up => up.UsuarioId == id).ToListAsync();
                _context.UsuarioPerfis.RemoveRange(vinculos);
                _context.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = id, PerfilId = perfil.Id });
            }

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _auditoria.RegistrarLog(UsuarioAtualId(), "Edição de Usuário", $"Usuário #{id} atualizado.", "Info", "API");
            return NoContent();
        }

        /// <summary>
        /// Realiza a exclusão e anonimização irreversível dos dados pessoais do colaborador em conformidade estrita com a LGPD.
        /// </summary>
        /// <param name="id">ID do colaborador a ser anonimizado.</param>
        /// <response code="204">Usuário desativado e dados anonimizados com sucesso.</response>
        /// <response code="400">Tentativa de autoexclusão da própria conta conectada.</response>
        /// <response code="404">Colaborador não encontrado.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            if (id == UsuarioAtualId()) return BadRequest(new { message = "A própria conta não pode ser excluída." });
            usuario.Nome = "Usuário anonimizado";
            usuario.Email = $"anonimizado-{usuario.Id}@invalid.local";
            usuario.CPF = string.Empty;
            usuario.RG = string.Empty;
            usuario.Telefone = string.Empty;
            usuario.Endereco = string.Empty;
            usuario.Cargo = string.Empty;
            usuario.Departamento = string.Empty;
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
            usuario.Ativo = false;
            await _context.SaveChangesAsync();
            await _auditoria.RegistrarLog(UsuarioAtualId(), "Anonimização de Usuário", $"Usuário #{id} anonimizado.", "Warning", "API");
            return NoContent();
        }

        private int UsuarioAtualId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private static bool SenhaValida(string senha) => senha.Length >= 8 && senha.Any(char.IsUpper) && senha.Any(char.IsLower) && senha.Any(char.IsDigit) && senha.Any(c => !char.IsLetterOrDigit(c));
    }
}
