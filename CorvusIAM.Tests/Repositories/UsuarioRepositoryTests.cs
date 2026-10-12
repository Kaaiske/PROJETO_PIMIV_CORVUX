using System.Linq;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CorvusIAM.Tests.Repositories
{
    public class UsuarioRepositoryTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public void Adicionar_ShouldAddUser()
        {
            // Arrange
            var context = GetDbContext();
            var repo = new UsuarioRepository(context);
            var usuario = new Usuario 
            { 
                Nome = "Teste", 
                Email = "teste@teste.com", 
                SenhaHash = "hash" 
            };

            // Act
            repo.Adicionar(usuario);

            // Assert
            var savedUser = context.Usuarios.FirstOrDefault(u => u.Email == "teste@teste.com");
            Assert.NotNull(savedUser);
            Assert.Equal("Teste", savedUser.Nome);
        }

        [Fact]
        public void ObterPorEmail_ShouldReturnUserWithProfiles()
        {
            // Arrange
            var context = GetDbContext();
            
            var perfil = new Perfil { Nome = "User" };
            var usuario = new Usuario 
            { 
                Nome = "Admin", 
                Email = "admin@teste.com", 
                SenhaHash = "hash" 
            };
            usuario.UsuarioPerfis.Add(new UsuarioPerfil { Perfil = perfil, Usuario = usuario });
            
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var repo = new UsuarioRepository(context);

            // Act
            var result = repo.ObterPorEmail("admin@teste.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin@teste.com", result.Email);
            Assert.Single(result.UsuarioPerfis);
            Assert.Equal("User", result.UsuarioPerfis.First().Perfil.Nome);
        }
    }
}
