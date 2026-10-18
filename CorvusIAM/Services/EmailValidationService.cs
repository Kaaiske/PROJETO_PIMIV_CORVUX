using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Services
{
    public class EmailValidationService : IEmailValidationService
    {
        private readonly HttpClient _httpClient;

        private static readonly HashSet<string> _disposableDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "mailinator.com", "tempmail.com", "10minutemail.com", "guerrillamail.com",
            "yopmail.com", "trashmail.com", "sharklasers.com", "getnada.com",
            "temp-mail.org", "throwawaymail.com", "fakeinbox.com", "maildrop.cc",
            "dispostable.com", "mailnesia.com", "mohmal.com"
        };

        private static readonly HashSet<string> _trustedDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com", "outlook.com", "hotmail.com", "live.com", "icloud.com",
            "yahoo.com", "yahoo.com.br", "uol.com.br", "bol.com.br", "terra.com.br",
            "corvux.com", "corvus.com", "corvuxiam.tech", "proton.me", "protonmail.com"
        };

        public EmailValidationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsDisposableEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return false;

            var domain = email.Split('@')[1].Trim().ToLowerInvariant();

            if (_disposableDomains.Contains(domain))
                return true;

            if (_trustedDomains.Contains(domain))
                return false;

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                var response = await _httpClient.GetAsync($"https://open.kickbox.com/v1/disposable/{domain}", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("disposable", out var disposableProp))
                    {
                        return disposableProp.GetBoolean();
                    }
                }
            }
            catch
            {
                // Fallback rápido sem bloquear a requisição do usuário
            }

            return false;
        }
    }
}
