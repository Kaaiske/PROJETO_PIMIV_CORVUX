using CorvusIAM.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace CorvusIAM.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;
        private readonly ITotpService? _totpService;
        private readonly IMemoryCache? _cache;

        private class MfaApiSession
        {
            public int UsuarioId { get; set; }
            public string Email { get; set; } = string.Empty;
            public int Tentativas { get; set; } = 0;
        }

        public AuthService(
            IUsuarioRepository usuarioRepository, 
            IConfiguration configuration,
            ITotpService? totpService = null,
            IMemoryCache? cache = null)
        {
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
            _totpService = totpService;
            _cache = cache;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public AuthResponse? Autenticar(string email, string senha)
        {
            var usuario = _usuarioRepository.ObterPorEmail(email);
            
            if (usuario == null || !usuario.Ativo)
                return null;

            if (usuario.DataDesbloqueio.HasValue && usuario.DataDesbloqueio.Value > DateTime.UtcNow)
                return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);
            if (!isPasswordValid)
            {
                usuario.TentativasLogin++;
                if (usuario.TentativasLogin >= 5)
                {
                    usuario.DataDesbloqueio = DateTime.UtcNow.AddMinutes(15);
                }
                _usuarioRepository.Atualizar(usuario);
                return null;
            }

            usuario.TentativasLogin = 0;
            usuario.DataDesbloqueio = null;

            // Se MFA estiver habilitado no perfil do usuário, intercepta e exige segundo fator
            if (usuario.MfaHabilitado && _totpService != null && _cache != null)
            {
                // Fail-closed: se não tiver segredo TOTP configurado, não permite login
                if (string.IsNullOrWhiteSpace(usuario.MfaSecretKey))
                {
                    return null;
                }

                var mfaSessionToken = "mfa_api_" + Guid.NewGuid().ToString("N");
                var session = new MfaApiSession
                {
                    UsuarioId = usuario.Id,
                    Email = usuario.Email,
                    Tentativas = 0
                };

                _cache.Set(mfaSessionToken, session, TimeSpan.FromMinutes(5));

                return new AuthResponse
                {
                    MfaRequerido = true,
                    MfaSessionToken = mfaSessionToken,
                    Token = string.Empty,
                    RefreshToken = string.Empty
                };
            }

            var refreshToken = GenerateRefreshToken();
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiracao = DateTime.UtcNow.AddDays(7);
            
            _usuarioRepository.Atualizar(usuario);

            return new AuthResponse 
            { 
                Token = GenerateJwt(usuario),
                RefreshToken = refreshToken,
                MfaRequerido = false
            };
        }

        public AuthResponse? AutenticarMfa(string mfaSessionToken, string codigoTotp)
        {
            if (_cache == null || _totpService == null || string.IsNullOrWhiteSpace(mfaSessionToken) || string.IsNullOrWhiteSpace(codigoTotp))
                return null;

            if (!_cache.TryGetValue(mfaSessionToken, out MfaApiSession? session) || session == null)
                return null;

            // Proteção contra brute force no código TOTP (máximo 5 tentativas por sessão temporária)
            if (session.Tentativas >= 5)
            {
                _cache.Remove(mfaSessionToken);
                return null;
            }

            var usuario = _usuarioRepository.ObterPorEmail(session.Email);
            if (usuario == null || !usuario.Ativo)
            {
                _cache.Remove(mfaSessionToken);
                return null;
            }

            if (string.IsNullOrWhiteSpace(usuario.MfaSecretKey))
            {
                _cache.Remove(mfaSessionToken);
                return null;
            }

            bool codigoValido = _totpService.ValidateCode(usuario.MfaSecretKey, codigoTotp.Trim());
            if (!codigoValido)
            {
                session.Tentativas++;
                _cache.Set(mfaSessionToken, session, TimeSpan.FromMinutes(5));
                return null;
            }

            // Sucesso na verificação MFA: limpa sessão temporária e emite credenciais
            _cache.Remove(mfaSessionToken);

            var refreshToken = GenerateRefreshToken();
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiracao = DateTime.UtcNow.AddDays(7);
            _usuarioRepository.Atualizar(usuario);

            return new AuthResponse
            {
                Token = GenerateJwt(usuario),
                RefreshToken = refreshToken,
                MfaRequerido = false
            };
        }

        public AuthResponse? AtualizarToken(string email, string refreshToken)
        {
            var usuario = _usuarioRepository.ObterPorEmail(email);
            if (usuario == null || !usuario.Ativo || usuario.RefreshToken != refreshToken || usuario.RefreshTokenExpiracao <= DateTime.UtcNow)
                return null;

            var newRefreshToken = GenerateRefreshToken();
            usuario.RefreshToken = newRefreshToken;
            usuario.RefreshTokenExpiracao = DateTime.UtcNow.AddDays(7);
            _usuarioRepository.Atualizar(usuario);

            return new AuthResponse 
            { 
                Token = GenerateJwt(usuario),
                RefreshToken = newRefreshToken,
                MfaRequerido = false
            };
        }

        private string GenerateJwt(CorvusIAM.Entities.Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyStr = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyStr)) {
                throw new Exception("JWT Key is missing from configuration.");
            }
            var key = Encoding.ASCII.GetBytes(keyStr);
            
            var claimsIdentity = new ClaimsIdentity(new[] 
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email)
            });

            if (usuario.UsuarioPerfis != null)
            {
                foreach (var up in usuario.UsuarioPerfis)
                {
                    if (up.Perfil != null)
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, up.Perfil.Nome));
                        if (up.Perfil.PerfilPermissoes != null)
                        {
                            foreach (var permissao in up.Perfil.PerfilPermissoes.Where(pp => pp.Permissao != null).Select(pp => pp.Permissao.Nome).Distinct())
                            {
                                claimsIdentity.AddClaim(new Claim("permission", permissao));
                            }
                        }
                    }
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
