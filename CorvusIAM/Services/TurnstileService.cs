using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CorvusIAM.Services
{
    public class TurnstileService : ITurnstileService
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _environment;
        private readonly string _secretKey;

        public TurnstileService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _httpClient = httpClient;
            _environment = environment;
            _secretKey = configuration["Turnstile:SecretKey"] 
                ?? configuration["TURNSTILE_SECRETKEY"] 
                ?? string.Empty;
        }

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp = null)
        {
            // Em ambiente de Desenvolvimento/Testes com chaves Cloudflare de teste (1x, 2x, 3x) ou sem token
            if (_environment.IsDevelopment())
            {
                if (string.IsNullOrEmpty(token) || 
                    string.IsNullOrWhiteSpace(_secretKey) ||
                    _secretKey.StartsWith("1x") ||
                    _secretKey.StartsWith("2x") ||
                    _secretKey.StartsWith("3x"))
                {
                    Serilog.Log.Information("Cloudflare Turnstile em modo de desenvolvimento (Bypass de teste autorizado).");
                    return true;
                }
            }

            // Em Produção: falha segura (Fail-Closed)
            if (string.IsNullOrWhiteSpace(token))
            {
                Serilog.Log.Warning("Tentativa de validação Turnstile com token vazio ou ausente.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                Serilog.Log.Error("Cloudflare Turnstile SecretKey não configurada em ambiente de produção!");
                return false;
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
                else
                {
                    Serilog.Log.Warning("Falha na chamada Turnstile. Status Code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro de comunicação ao validar Cloudflare Turnstile. Falha segura aplicada.");
                // Fail-Closed: Em produção, falha de comunicação bloqueia requisição não verificada
                return false;
            }

            return false;
        }
    }
}
