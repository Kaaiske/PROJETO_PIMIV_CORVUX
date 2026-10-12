using CorvusIAM.Interfaces;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CorvusIAM.Services
{
    public class PasswordGeneratorService : IPasswordGeneratorService
    {
        private readonly IPasswordPwnedService _passwordPwned;

        public PasswordGeneratorService(IPasswordPwnedService passwordPwned)
        {
            _passwordPwned = passwordPwned;
        }

        public async Task<string> GenerateSecurePasswordAsync(int length = 14)
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
            string senha = "";
            bool isPwned = true;
            
            while (isPwned)
            {
                senha = string.Concat(Enumerable.Range(0, length).Select(_ => caracteres[RandomNumberGenerator.GetInt32(caracteres.Length)]));
                isPwned = await _passwordPwned.IsPasswordPwnedAsync(senha);
            }
            return senha;
        }
    }
}
