using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface IPasswordPwnedService
    {
        Task<bool> IsPasswordPwnedAsync(string password);
    }
}
