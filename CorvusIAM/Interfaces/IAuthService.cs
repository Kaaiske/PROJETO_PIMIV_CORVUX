namespace CorvusIAM.Interfaces
{
    public class AuthResponse 
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool MfaRequerido { get; set; } = false;
        public string? MfaSessionToken { get; set; }
    }

    public interface IAuthService
    {
        AuthResponse? Autenticar(string email, string senha);
        AuthResponse? AutenticarMfa(string mfaSessionToken, string codigoTotp);
        AuthResponse? AtualizarToken(string email, string refreshToken);
    }
}
