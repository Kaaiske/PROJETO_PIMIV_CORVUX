using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CorvusIAM.Services
{
    public class IpGeolocationService : IIpGeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public IpGeolocationService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
            _httpClient.Timeout = TimeSpan.FromMilliseconds(1500);
        }

        public async Task<string> GetLocationFromIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || 
                ipAddress == "::1" || 
                ipAddress == "127.0.0.1" || 
                ipAddress.StartsWith("192.168.") || 
                ipAddress.StartsWith("10.") || 
                ipAddress.StartsWith("172.") || 
                ipAddress.StartsWith("169.254."))
            {
                return "Rede Corporativa (Intranet)";
            }

            var cacheKey = $"geo_{ipAddress}";
            if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            {
                return cached;
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1200));
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}?fields=status,country,city", cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("status", out var status) && status.GetString() == "success")
                    {
                        var country = doc.RootElement.GetProperty("country").GetString();
                        var city = doc.RootElement.GetProperty("city").GetString();
                        var result = $"{city}, {country}";
                        _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
                        return result;
                    }
                }
            }
            catch
            {
                // Timeout ou falha externa não bloqueia o fluxo
            }

            var fallback = "São Paulo, Brasil";
            _cache.Set(cacheKey, fallback, TimeSpan.FromHours(6));
            return fallback;
        }
    }
}
