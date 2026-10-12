using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface ITotpService
    {
        string GenerateSecretKey();
        string GenerateQrCodeUri(string email, string secretKey);
        string GenerateQrCodeImageUrl(string email, string secretKey);
        bool ValidateCode(string secretKey, string code);
    }
}
