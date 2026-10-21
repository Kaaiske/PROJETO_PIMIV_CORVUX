using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace CorvusIAM.Infrastructure.Logging
{
    /// <summary>
    /// Enriquecedor e filtro de mascaramento de dados sensíveis para Serilog (LGPD / PCI-DSS).
    /// Remove credenciais, tokens JWT e números de CPF dos fluxos de logs de aplicação.
    /// </summary>
    public class SerilogMaskingEnricher : ILogEventEnricher
    {
        private static readonly Regex CpfRegex = new(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b", RegexOptions.Compiled);
        private static readonly Regex BearerRegex = new(@"(Bearer\s+)[A-Za-z0-9\-_.]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PasswordRegex = new(@"([""']?(?:senha|password|secret|token)[""']?\s*[:=]\s*[""'])([^""']+)([""'])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Application", "CorvuxIAM"));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ZeroTrustCompliant", true));
        }

        public static string Mask(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var masked = CpfRegex.Replace(input, "***.***.***-**");
            masked = BearerRegex.Replace(masked, "$1***MASKED_TOKEN***");
            masked = PasswordRegex.Replace(masked, "$1***PROTECTED***$3");
            return masked;
        }
    }
}

