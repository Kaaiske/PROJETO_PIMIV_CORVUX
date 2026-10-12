using CorvusIAM.Entities;

namespace CorvusIAM.Interfaces
{
    public interface IUsuarioRepository
    {
        Usuario? ObterPorEmail(string email);
        void Adicionar(Usuario usuario);
        void Atualizar(Usuario usuario);
    }
}
