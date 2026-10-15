using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface ITurnstileService
    {
        Task<bool> VerifyTokenAsync(string token, string? remoteIp = null);
    }
}
