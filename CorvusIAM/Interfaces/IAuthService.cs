namespace CorvusIAM.Interfaces
{
    public class AuthResponse 
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public interface IAuthService
    {
        AuthResponse? Autenticar(string email, string senha);
        AuthResponse? AtualizarToken(string email, string refreshToken);
    }
}
