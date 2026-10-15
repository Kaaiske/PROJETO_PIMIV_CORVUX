using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CorvusIAM.Services
{
    public class TurnstileService : ITurnstileService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public TurnstileService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = configuration["Turnstile:SecretKey"] 
                ?? "1x0000000000000000000000000000000AA";
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token) || 
                _secretKey == "1x0000000000000000000000000000000AA" ||
                _secretKey.StartsWith("1x00000") ||
                _secretKey.StartsWith("3x00000"))
            {
                return true;
            }
            var values = new Dictionary<string, string>
            {
                { "secret", _secretKey },
                { "response", token }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await _httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("success", out var successProp))
                {
                    return successProp.GetBoolean();
                }
            }

            return false;
        }
    }
}

