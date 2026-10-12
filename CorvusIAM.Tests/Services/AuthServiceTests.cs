using System;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using CorvusIAM.Services;
using CorvusIAM.Interfaces;
using CorvusIAM.Entities;
using System.Collections.Generic;

namespace CorvusIAM.Tests.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public void Autenticar_RetornaToken_QuandoCredenciaisSaoValidas()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            var mockConfig = new Mock<IConfiguration>();

            string email = "test@corvus.com";
            string password = "TestPassword123!";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var usuario = new Usuario
            {
                Id = 1,
                Email = email,
                SenhaHash = passwordHash,
                Ativo = true,
                UsuarioPerfis = new List<UsuarioPerfil>
                {
                    new UsuarioPerfil { Perfil = new Perfil { Nome = "Admin" } }
                }
            };

            mockRepo.Setup(r => r.ObterPorEmail(email)).Returns(usuario);
            mockConfig.Setup(c => c["Jwt:Key"]).Returns("UmaChaveSecretaMuitoLongaEValidaParaOJwtTokenTest");
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            var authService = new AuthService(mockRepo.Object, mockConfig.Object);

            var token = authService.Autenticar(email, password);

            Assert.NotNull(token);
        }

        [Fact]
        public void Autenticar_RetornaNull_QuandoUsuarioNaoEncontrado()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            var mockConfig = new Mock<IConfiguration>();

            string email = "invalid@corvus.com";
            
            mockRepo.Setup(r => r.ObterPorEmail(email)).Returns((Usuario)null);

            var authService = new AuthService(mockRepo.Object, mockConfig.Object);

            var token = authService.Autenticar(email, "password");

            Assert.Null(token);
        }

        [Fact]
        public void Autenticar_RetornaNull_QuandoSenhaForInvalida()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            var mockConfig = new Mock<IConfiguration>();

            string email = "test@corvus.com";
            string password = "TestPassword123!";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var usuario = new Usuario
            {
                Id = 1,
                Email = email,
                SenhaHash = passwordHash,
                Ativo = true
            };

            mockRepo.Setup(r => r.ObterPorEmail(email)).Returns(usuario);

            var authService = new AuthService(mockRepo.Object, mockConfig.Object);

            var token = authService.Autenticar(email, "WrongPassword");

            Assert.Null(token);
        }

        [Fact]
        public void Autenticar_RetornaNull_QuandoUsuarioEstaInativo()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            var mockConfig = new Mock<IConfiguration>();

            string email = "test@corvus.com";
            string password = "TestPassword123!";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var usuario = new Usuario
            {
                Id = 1,
                Email = email,
                SenhaHash = passwordHash,
                Ativo = false
            };

            mockRepo.Setup(r => r.ObterPorEmail(email)).Returns(usuario);

            var authService = new AuthService(mockRepo.Object, mockConfig.Object);

            var token = authService.Autenticar(email, password);

            Assert.Null(token);
        }
    }
}

