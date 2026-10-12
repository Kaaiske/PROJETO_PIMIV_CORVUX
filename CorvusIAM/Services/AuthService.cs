using CorvusIAM.Interfaces;
using Microsoft.Extensions.Configuration;
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

        public AuthService(IUsuarioRepository usuarioRepository, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
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

            var refreshToken = GenerateRefreshToken();
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiracao = DateTime.UtcNow.AddDays(7);
            
            _usuarioRepository.Atualizar(usuario);

            return new AuthResponse 
            { 
                Token = GenerateJwt(usuario),
                RefreshToken = refreshToken
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
                RefreshToken = newRefreshToken
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

            foreach (var up in usuario.UsuarioPerfis)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, up.Perfil.Nome));
                foreach (var permissao in up.Perfil.PerfilPermissoes.Select(pp => pp.Permissao.Nome).Distinct())
                {
                    claimsIdentity.AddClaim(new Claim("permission", permissao));
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
