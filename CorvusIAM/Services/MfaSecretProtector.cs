using CorvusIAM.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace CorvusIAM.Services;

public sealed class MfaSecretProtector : IMfaSecretProtector
{
    private const string Prefix = "v1:";
    private readonly IDataProtector _protector;

    public MfaSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("CorvusIAM.MfaSecret.v1");
    }

    public string Protect(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        return Prefix + _protector.Protect(secret);
    }

    public string? Unprotect(string? protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
            return protectedSecret;

        if (!protectedSecret.StartsWith(Prefix, StringComparison.Ordinal))
            return protectedSecret;

        try
        {
            return _protector.Unprotect(protectedSecret[Prefix.Length..]);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    public bool IsProtected(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith(Prefix, StringComparison.Ordinal);
}
