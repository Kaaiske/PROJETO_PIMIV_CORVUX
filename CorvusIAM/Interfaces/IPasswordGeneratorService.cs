using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface IPasswordGeneratorService
    {
        Task<string> GenerateSecurePasswordAsync(int length = 14);
    }
}
