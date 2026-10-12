using System.Threading.Tasks;
using CorvusIAM.Entities;

namespace CorvusIAM.Interfaces
{
    public interface IUsuarioService
    {
        Task<Usuario> CreateUsuarioAsync(Usuario usuario, string rawPassword, int? loggedInUserId);
        Task<Usuario> UpdateUsuarioAsync(Usuario usuario, int? loggedInUserId);
        Task DeleteUsuarioAsync(int usuarioId, int? loggedInUserId);
        Task BlockUsuarioAsync(int usuarioId, int? loggedInUserId);
    }
}
