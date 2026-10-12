using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CorvusIAM.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;
        public UsuarioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Adicionar(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();
        }

        public void Atualizar(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            _context.SaveChanges();
        }

        public Usuario? ObterPorEmail(string email)
        {
            return _context.Usuarios
                .Include(u => u.UsuarioPerfis)
                .ThenInclude(up => up.Perfil)
                .ThenInclude(p => p.PerfilPermissoes)
                .ThenInclude(pp => pp.Permissao)
                .FirstOrDefault(u => u.Email == email);
        }
    }
}
