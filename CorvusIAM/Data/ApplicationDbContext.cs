using Microsoft.EntityFrameworkCore;
using CorvusIAM.Entities;
using System;

namespace CorvusIAM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Perfil> Perfis { get; set; }
        public DbSet<Permissao> Permissoes { get; set; }
        public DbSet<UsuarioPerfil> UsuarioPerfis { get; set; }
        public DbSet<PerfilPermissao> PerfilPermissoes { get; set; }
        public DbSet<PoliticaAcesso> PoliticasAcesso { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }
        public DbSet<ChamadoSuporte> ChamadosSuporte { get; set; }
        public DbSet<ConfiguracaoSistema> ConfiguracoesSistema { get; set; }
        public DbSet<FirewallRegra> FirewallRegras { get; set; }
        public DbSet<DispositivoRede> DispositivosRede { get; set; }
        public DbSet<NotaFiscal> NotasFiscais { get; set; }
        public DbSet<FechamentoMensal> FechamentosMensais { get; set; }
        public DbSet<RegistroFerias> RegistrosFerias { get; set; }
        public DbSet<FolhaPagamento> FolhasPagamento { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsuarioPerfil>()
                .HasKey(up => new { up.UsuarioId, up.PerfilId });

            modelBuilder.Entity<UsuarioPerfil>()
                .HasOne(up => up.Usuario)
                .WithMany(u => u.UsuarioPerfis)
                .HasForeignKey(up => up.UsuarioId);

            modelBuilder.Entity<UsuarioPerfil>()
                .HasOne(up => up.Perfil)
                .WithMany(p => p.UsuarioPerfis)
                .HasForeignKey(up => up.PerfilId);

            modelBuilder.Entity<PerfilPermissao>()
                .HasKey(pp => new { pp.PerfilId, pp.PermissaoId });

            modelBuilder.Entity<PerfilPermissao>()
                .HasOne(pp => pp.Perfil)
                .WithMany(p => p.PerfilPermissoes)
                .HasForeignKey(pp => pp.PerfilId);

            modelBuilder.Entity<PerfilPermissao>()
                .HasOne(pp => pp.Permissao)
                .WithMany(p => p.PerfilPermissoes)
                .HasForeignKey(pp => pp.PermissaoId);
                
            modelBuilder.Entity<ChamadoSuporte>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId);

            modelBuilder.Entity<ChamadoSuporte>()
                .HasOne(c => c.Responsavel)
                .WithMany()
                .HasForeignKey(c => c.ResponsavelId)
                .OnDelete(DeleteBehavior.NoAction);

            // Seed Data
            modelBuilder.Entity<Perfil>().HasData(
                new Perfil { Id = 1, Nome = "Admin", Descricao = "Administrador do Sistema", Ativo = true },
                new Perfil { Id = 2, Nome = "Recursos Humanos", Descricao = "Recursos Humanos (RH)", Ativo = true },
                new Perfil { Id = 3, Nome = "Contabilidade", Descricao = "Contabilidade", Ativo = true },
                new Perfil { Id = 4, Nome = "Help Desk", Descricao = "Suporte de TI / Help Desk", Ativo = true },
                new Perfil { Id = 5, Nome = "Colaborador", Descricao = "Usuário Padrão", Ativo = true }
            );

            modelBuilder.Entity<Permissao>().HasData(
                new Permissao { Id = 1, Nome = "usuarios.visualizar", Descricao = "Visualizar usuários" },
                new Permissao { Id = 2, Nome = "usuarios.gerenciar", Descricao = "Gerenciar usuários" },
                new Permissao { Id = 3, Nome = "perfis.gerenciar", Descricao = "Gerenciar perfis" },
                new Permissao { Id = 4, Nome = "chamados.atender", Descricao = "Atender chamados" },
                new Permissao { Id = 5, Nome = "logs.visualizar", Descricao = "Visualizar auditoria" },
                new Permissao { Id = 6, Nome = "politicas.gerenciar", Descricao = "Gerenciar políticas" }
            );

            modelBuilder.Entity<PerfilPermissao>().HasData(
                new PerfilPermissao { PerfilId = 1, PermissaoId = 1 },
                new PerfilPermissao { PerfilId = 1, PermissaoId = 2 },
                new PerfilPermissao { PerfilId = 1, PermissaoId = 3 },
                new PerfilPermissao { PerfilId = 1, PermissaoId = 4 },
                new PerfilPermissao { PerfilId = 1, PermissaoId = 5 },
                new PerfilPermissao { PerfilId = 1, PermissaoId = 6 },
                new PerfilPermissao { PerfilId = 2, PermissaoId = 1 },
                new PerfilPermissao { PerfilId = 2, PermissaoId = 2 },
                new PerfilPermissao { PerfilId = 4, PermissaoId = 4 },
                new PerfilPermissao { PerfilId = 5, PermissaoId = 1 }
            );

            // Hash estável de "Cv1670!" para evitar alterações artificiais no modelo a cada migration.
            const string senhaHash = "$2a$11$y4AkJtTDlULmPEEKkqBLk.MM4ZKyMK8ExkIkfyAY5s/iEMDL1UvIS";

            modelBuilder.Entity<Usuario>().HasData(new Usuario {
                Id = 1,
                Nome = "Administrador TI",
                Email = "admin@corvus.com",
                SenhaHash = senhaHash,
                Ativo = true,
                CPF = "000.000.000-00",
                Cargo = "CTO",
                DataNascimento = new DateTime(1990, 1, 1),
                DataContratacao = new DateTime(2023, 1, 1),
                RG = "00.000.000-0",
                Telefone = "(11) 99999-9999",
                Departamento = "Tecnologia",
                Endereco = "Av. Paulista, 1000 - SP",
                MfaHabilitado = false,
                MfaSecretKey = string.Empty
            });

            modelBuilder.Entity<UsuarioPerfil>().HasData(
                new UsuarioPerfil { UsuarioId = 1, PerfilId = 1 }
            );
        }
    }
}

