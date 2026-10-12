using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface ILogAuditoriaService
    {
        Task RegistrarLog(int? usuarioId, string acao, string detalhe, string tipo, string origem);
    }
}
