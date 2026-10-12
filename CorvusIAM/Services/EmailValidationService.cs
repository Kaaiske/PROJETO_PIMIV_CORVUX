using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Services
{
    public class EmailValidationService : IEmailValidationService
    {
        private readonly HttpClient _httpClient;

        public EmailValidationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsDisposableEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return false;

            var domain = email.Split('@')[1].ToLower();

            try
            {
                // Kickbox open API for checking disposable domains (Free)
                var response = await _httpClient.GetAsync($"https://open.kickbox.com/v1/disposable/{domain}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("disposable", out var disposableProp))
                    {
                        return disposableProp.GetBoolean();
                    }
                }
            }
            catch
            {
                // Fallback to false if API fails
            }

            return false;
        }
    }
}
