namespace CorvusIAM.Interfaces;

public interface IMfaSecretProtector
{
    string Protect(string secret);
    string? Unprotect(string? protectedSecret);
    bool IsProtected(string? value);
}
