using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Services
{
    public class IpGeolocationService : IIpGeolocationService
    {
        private readonly HttpClient _httpClient;

        public IpGeolocationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetLocationFromIpAsync(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                return "Localhost";
            }

            try
            {
                // ip-api.com returns JSON, 100% free for non-commercial without API key
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.GetProperty("status").GetString() == "success")
                    {
                        var country = doc.RootElement.GetProperty("country").GetString();
                        var city = doc.RootElement.GetProperty("city").GetString();
                        return $"{city}, {country}";
                    }
                }
            }
            catch
            {
                // Log exception if needed, silently fail for now
            }

            return "Unknown";
        }
    }
}
