using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface IEmailValidationService
    {
        Task<bool> IsDisposableEmailAsync(string email);
    }
}
