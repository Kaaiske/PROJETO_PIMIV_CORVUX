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
                ?? "0x4AAAAAAFXK5H5jTPKYIrdR2p0kwwYhL1Y";
        }

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp = null)
        {
            // Em ambiente com chaves de teste Cloudflare (1x, 2x, 3x) ou sem token exigido, aprova
            if (string.IsNullOrEmpty(token) || 
                string.IsNullOrWhiteSpace(_secretKey) ||
                _secretKey.StartsWith("1x") ||
                _secretKey.StartsWith("2x") ||
                _secretKey.StartsWith("3x"))
            {
                return true;
            }

            try
            {
                var values = new Dictionary<string, string>
                {
                    { "secret", _secretKey },
                    { "response", token }
                };

                if (!string.IsNullOrEmpty(remoteIp))
                {
                    values.Add("remoteip", remoteIp);
                }

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
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "Erro de comunicação ao validar Cloudflare Turnstile");
                // Resiliência de infraestrutura: não derrubar login se Cloudflare estiver temporariamente inacessível
                return true;
            }

            return false;
        }
    }
}

