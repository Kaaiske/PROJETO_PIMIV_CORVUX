using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Services
{
    public class PasswordPwnedService : IPasswordPwnedService
    {
        private readonly HttpClient _httpClient;

        public PasswordPwnedService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CorvusIAM-App");
        }

        public async Task<bool> IsPasswordPwnedAsync(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            try
            {
                // Generate SHA1 hash
                using var sha1 = SHA1.Create();
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

                var prefix = hashString.Substring(0, 5);
                var suffix = hashString.Substring(5);

                var response = await _httpClient.GetAsync($"range/{prefix}");
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to false if API fails
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                return content.Contains(suffix);
            }
            catch
            {
                // Fallback seguro se serviço externo estiver indisponível
                return false;
            }
        }
    }
}
