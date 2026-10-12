using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogAuditoriaService _logAuditoriaService;

        public UsuarioService(ApplicationDbContext context, ILogAuditoriaService logAuditoriaService)
        {
            _context = context;
            _logAuditoriaService = logAuditoriaService;
        }

        public async Task<Usuario> CreateUsuarioAsync(Usuario usuario, string rawPassword, int? loggedInUserId)
        {
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            await _logAuditoriaService.RegistrarLog(loggedInUserId, "Criar Usuário", $"Usuário {usuario.Email} criado com sucesso.", "Segurança", "UsuarioService");
            
            return usuario;
        }

        public async Task<Usuario> UpdateUsuarioAsync(Usuario usuario, int? loggedInUserId)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            await _logAuditoriaService.RegistrarLog(loggedInUserId, "Atualizar Usuário", $"Usuário {usuario.Email} atualizado com sucesso.", "Segurança", "UsuarioService");

            return usuario;
        }

        public async Task DeleteUsuarioAsync(int usuarioId, int? loggedInUserId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                await _logAuditoriaService.RegistrarLog(loggedInUserId, "Excluir Usuário", $"Usuário {usuario.Email} excluído com sucesso.", "Segurança", "UsuarioService");
            }
        }

        public async Task BlockUsuarioAsync(int usuarioId, int? loggedInUserId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.Ativo = false;
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                await _logAuditoriaService.RegistrarLog(loggedInUserId, "Bloquear Usuário", $"Usuário {usuario.Email} bloqueado com sucesso.", "Segurança", "UsuarioService");
            }
        }
    }
}

