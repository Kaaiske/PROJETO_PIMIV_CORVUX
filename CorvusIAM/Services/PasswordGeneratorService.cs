using CorvusIAM.Interfaces;
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

        public Task<string> GenerateSecurePasswordAsync(int length = 14)
        {
            if (length < 8) length = 14;
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "!@#$%&*";
            const string all = upper + lower + digits + specials;

            char[] buffer = new char[length];
            buffer[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            buffer[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            buffer[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            buffer[3] = specials[RandomNumberGenerator.GetInt32(specials.Length)];

            for (int i = 4; i < length; i++)
            {
                buffer[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            // Embaralhamento Fisher-Yates criptograficamente seguro (execução < 0.1ms sem latência de rede)
            for (int i = length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }

            return Task.FromResult(new string(buffer));
        }
    }
}
