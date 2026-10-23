using CorvusIAM.Interfaces;
using CorvusIAM.Data;
using CorvusIAM.Entities;
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
        private readonly IMfaSecretProtector? _mfaSecretProtector;
        private readonly ApplicationDbContext? _context;

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
            IMemoryCache? cache = null,
            IMfaSecretProtector? mfaSecretProtector = null,
            ApplicationDbContext? context = null)
        {
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
            _totpService = totpService;
            _cache = cache;
            _mfaSecretProtector = mfaSecretProtector;
            _context = context;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

        private string StoreRefreshToken(string rawToken) => HashToken(rawToken);

        private string? ReadMfaSecret(string? value) => _mfaSecretProtector?.Unprotect(value) ?? value;

        public AuthResponse? Autenticar(string email, string senha)
        {
            var usuario = _usuarioRepository.ObterPorEmail(email.Trim().ToLowerInvariant());
            
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
            if (usuario.MfaHabilitado && _totpService != null && (_cache != null || _context != null))
            {
                // Fail-closed: se não tiver segredo TOTP configurado, não permite login
                if (string.IsNullOrWhiteSpace(ReadMfaSecret(usuario.MfaSecretKey)))
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

                if (_context != null)
                {
                    _context.MfaSessoes.Add(new MfaSession
                    {
                        TokenHash = HashToken(mfaSessionToken),
                        UsuarioId = usuario.Id,
                        Email = usuario.Email,
                        ExpiraEm = DateTime.UtcNow.AddMinutes(5),
                        Canal = "API"
                    });
                    _context.SaveChanges();
                }
                else
                {
                    _cache!.Set(mfaSessionToken, session, TimeSpan.FromMinutes(5));
                }

                return new AuthResponse
                {
                    MfaRequerido = true,
                    MfaSessionToken = mfaSessionToken,
                    Token = string.Empty,
                    RefreshToken = string.Empty
                };
            }

            var refreshToken = GenerateRefreshToken();
            usuario.RefreshToken = StoreRefreshToken(refreshToken);
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
            if (_totpService == null || string.IsNullOrWhiteSpace(mfaSessionToken) || string.IsNullOrWhiteSpace(codigoTotp))
                return null;

            MfaSession? persistedSession = null;
            MfaApiSession? session = null;
            if (_context != null)
            {
                persistedSession = _context.MfaSessoes.FirstOrDefault(s => s.TokenHash == HashToken(mfaSessionToken));
                if (persistedSession == null || persistedSession.ExpiraEm <= DateTime.UtcNow)
                    return null;
            }
            else if (_cache == null || !_cache.TryGetValue(mfaSessionToken, out session) || session == null)
                return null;

            // Proteção contra brute force no código TOTP (máximo 5 tentativas por sessão temporária)
            var attempts = persistedSession?.Tentativas ?? session!.Tentativas;
            if (attempts >= 5)
            {
                if (persistedSession != null) _context!.MfaSessoes.Remove(persistedSession);
                else _cache!.Remove(mfaSessionToken);
                _context?.SaveChanges();
                return null;
            }

            var usuario = _usuarioRepository.ObterPorEmail(persistedSession?.Email ?? session!.Email);
            if (usuario == null || !usuario.Ativo)
            {
                if (persistedSession != null) _context!.MfaSessoes.Remove(persistedSession);
                else _cache!.Remove(mfaSessionToken);
                _context?.SaveChanges();
                return null;
            }

            var mfaSecret = ReadMfaSecret(usuario.MfaSecretKey);
            if (string.IsNullOrWhiteSpace(mfaSecret))
            {
                if (persistedSession != null) _context!.MfaSessoes.Remove(persistedSession);
                else _cache!.Remove(mfaSessionToken);
                _context?.SaveChanges();
                return null;
            }

            bool codigoValido = _totpService.ValidateCode(mfaSecret, codigoTotp.Trim());
            if (!codigoValido)
            {
                if (persistedSession != null)
                    persistedSession.Tentativas++;
                else
                {
                    session!.Tentativas++;
                    _cache!.Set(mfaSessionToken, session, TimeSpan.FromMinutes(5));
                }
                _context?.SaveChanges();
                return null;
            }

            // Sucesso na verificação MFA: limpa sessão temporária e emite credenciais
            if (persistedSession != null) _context!.MfaSessoes.Remove(persistedSession);
            else _cache!.Remove(mfaSessionToken);
            _context?.SaveChanges();

            var refreshToken = GenerateRefreshToken();
            usuario.RefreshToken = StoreRefreshToken(refreshToken);
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
            var usuario = _usuarioRepository.ObterPorEmail(email.Trim().ToLowerInvariant());
            var suppliedHash = HashToken(refreshToken);
            if (usuario == null || !usuario.Ativo || string.IsNullOrWhiteSpace(usuario.RefreshToken) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(usuario.RefreshToken),
                    Encoding.ASCII.GetBytes(suppliedHash)) ||
                usuario.RefreshTokenExpiracao <= DateTime.UtcNow)
                return null;

            var newRefreshToken = GenerateRefreshToken();
            usuario.RefreshToken = StoreRefreshToken(newRefreshToken);
            usuario.RefreshTokenExpiracao = DateTime.UtcNow.AddDays(7);
            _usuarioRepository.Atualizar(usuario);

            return new AuthResponse 
            { 
                Token = GenerateJwt(usuario),
                RefreshToken = newRefreshToken,
                MfaRequerido = false
            };
        }

        public bool RevogarToken(string email, string refreshToken)
        {
            var usuario = _usuarioRepository.ObterPorEmail(email.Trim().ToLowerInvariant());
            if (usuario == null || string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(usuario.RefreshToken))
                return false;

            var suppliedHash = HashToken(refreshToken);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(usuario.RefreshToken),
                    Encoding.ASCII.GetBytes(suppliedHash)))
                return false;

            usuario.RefreshToken = string.Empty;
            usuario.RefreshTokenExpiracao = null;
            _usuarioRepository.Atualizar(usuario);
            return true;
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
                Expires = DateTime.UtcNow.AddHours(GetJwtExpirationHours()),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private double GetJwtExpirationHours()
        {
            var configured = _configuration["JWT_EXPIRATION_HOURS"]
                ?? _configuration["Jwt:ExpirationHours"]
                ?? _context?.ConfiguracoesSistema.FirstOrDefault(c => c.Chave == "JWT_EXPIRATION_HOURS")?.Valor;

            return double.TryParse(configured, out var hours)
                ? Math.Clamp(hours, 1, 24)
                : 2;
        }
    }
}
