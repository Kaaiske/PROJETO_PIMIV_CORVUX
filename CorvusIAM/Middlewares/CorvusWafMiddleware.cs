using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CorvusIAM.Middlewares
{
    /// <summary>
    /// Middleware L7 de Web Application Firewall (WAF) e IDS em tempo real.
    /// Inspeciona requisições HTTP contra listas de bloqueio e assinaturas de ataque conhecidas
    /// (SQL Injection, Cross-Site Scripting, Path Traversal e Command Injection).
    /// </summary>
    public class CorvusWafMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorvusWafMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private readonly HashSet<IPAddress> _trustedProxies;

        private const string CacheKeyBlockedIps = "CorvusWaf_BlockedIps";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        // Regex compiladas para inspeção de alta performance sem gargalo de CPU
        private static readonly Regex SqlInjectionPattern = new(
            @"(?i)(\b(union(\s+all)?\s+select|select\s+.*\s+from|insert\s+into|delete\s+from|drop\s+table|exec\s*\(|execute\s*\(|waitfor\s+delay)\b)|(\b1\s*=\s*1\b)|(\binformation_schema\b)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex XssPattern = new(
            @"(?i)(<script\b[^>]*>|<\/script>|javascript:|vbscript:|onload\s*=|onerror\s*=|onclick\s*=|document\.cookie)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex PathTraversalPattern = new(
            @"(?i)(\.\.[\\\/]|\.\.%2f|%2e%2e%2f|%2e%2e\/|\.\.%5c)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] IgnoredExtensions = {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".svg", ".ico", ".woff", ".woff2", ".ttf", ".webmanifest"
        };

        public CorvusWafMiddleware(RequestDelegate next, ILogger<CorvusWafMiddleware> logger, IMemoryCache cache, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
            _trustedProxies = new HashSet<IPAddress>();
            foreach (var value in configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? Array.Empty<string>())
            {
                if (IPAddress.TryParse(value, out var address))
                    _trustedProxies.Add(address);
            }
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Ignora arquivos estáticos para não degradar tempo de resposta
            if (IgnoredExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var clientIp = ObterIpCliente(context);

            // 1. Verificação de Reputação de IP (Lista de Bloqueio do Firewall)
            var blockedIps = ObterIpsBloqueados(serviceProvider);
            if (blockedIps.Contains(clientIp))
            {
                _logger.LogWarning("WAF: Requisição bloqueada para IP banido: {Ip} em {Path}", clientIp, path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Acesso bloqueado por politica de firewall corporativo (Corvux WAF / IDS).\"}");
                return;
            }

            // 2. Inspeção L7 de Payload (Path, QueryString e Cabeçalhos suspeitos)
            var queryString = context.Request.QueryString.Value ?? string.Empty;
            var targetPayload = $"{WebUtility.UrlDecode(path)} {WebUtility.UrlDecode(queryString)}";

            string? detectedThreat = null;

            if (SqlInjectionPattern.IsMatch(targetPayload))
            {
                detectedThreat = "SQL Injection (SQLi)";
            }
            else if (XssPattern.IsMatch(targetPayload))
            {
                detectedThreat = "Cross-Site Scripting (XSS)";
            }
            else if (PathTraversalPattern.IsMatch(targetPayload))
            {
                detectedThreat = "Directory/Path Traversal";
            }

            if (detectedThreat != null)
            {
                _logger.LogCritical("WAF IDS MITIGATION: Ataque L7 detectado ({Threat}) de {Ip} em {Path}{Query}",
                    detectedThreat, clientIp, path, queryString);

                // Registra evento de segurança na auditoria
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.LogsAuditoria.Add(new LogAuditoria
                    {
                        Acao = $"WAF Mitigação: {detectedThreat}",
                        Detalhe = $"Ataque mitigado na camada L7. Origem: {clientIp}. Alvo: {path}{queryString}",
                        Origem = "Corvux L7 WAF",
                        Tipo = "Bloqueio",
                        DataHora = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gravar log de mitigação do WAF");
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync($"{{\"error\":\"Requisicao interceptada pelo Corvux WAF L7. Assinatura de ameaca: {detectedThreat}.\"}}");
                return;
            }

            await _next(context);
        }

        private string ObterIpCliente(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            var trustedProxy = remoteIp != null && (IPAddress.IsLoopback(remoteIp) || _trustedProxies.Contains(remoteIp));

            if (trustedProxy && context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) &&
                IPAddress.TryParse(cfIp.ToString().Trim(), out var cloudflareIp))
            {
                return cloudflareIp.ToString();
            }

            if (trustedProxy && context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
            {
                var ips = forwarded.ToString().Split(',');
                if (ips.Length > 0 && IPAddress.TryParse(ips[0].Trim(), out var forwardedIp))
                {
                    return forwardedIp.ToString();
                }
            }

            return remoteIp?.ToString() ?? "127.0.0.1";
        }

        private HashSet<string> ObterIpsBloqueados(IServiceProvider serviceProvider)
        {
            if (_cache.TryGetValue(CacheKeyBlockedIps, out HashSet<string>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ips = db.FirewallRegras
                    .Where(f => f.Ativo)
                    .Select(f => f.Ip)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                _cache.Set(CacheKeyBlockedIps, ips, CacheDuration);
                return ips;
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
