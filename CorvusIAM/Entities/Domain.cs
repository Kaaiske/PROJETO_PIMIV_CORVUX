using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorvusIAM.Entities
{
    public class Usuario
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Email { get; set; } = string.Empty;
        [Required] public string SenhaHash { get; set; } = string.Empty;
        
        public bool Ativo { get; set; } = true;

        // Controle de Lockout e Reset de Senha
        public int TentativasLogin { get; set; } = 0;
        public DateTime? DataDesbloqueio { get; set; }
        
        public string? ResetSenhaToken { get; set; } = string.Empty;
        public DateTime? ResetSenhaExpiracao { get; set; }

        public string? RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiracao { get; set; }
        
        // Autenticação Multifator (MFA / 2FA)
        public bool MfaHabilitado { get; set; } = false;
        public string? MfaSecretKey { get; set; } = string.Empty;
        
        // Dados Pessoais
        [MaxLength(14)] public string? CPF { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }
        [MaxLength(100)] public string? Cargo { get; set; } = string.Empty;
        public DateTime? DataContratacao { get; set; }
        [MaxLength(20)] public string? RG { get; set; } = string.Empty;
        [MaxLength(20)] public string? Telefone { get; set; } = string.Empty;
        [MaxLength(100)] public string? Departamento { get; set; } = string.Empty;
        [MaxLength(200)] public string? Endereco { get; set; } = string.Empty;

        // Navegação
        public ICollection<UsuarioPerfil> UsuarioPerfis { get; set; } = new List<UsuarioPerfil>();
    }

    public class Perfil
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string Nome { get; set; } = string.Empty;
        [MaxLength(200)] public string? Descricao { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        // Navegação
        public ICollection<UsuarioPerfil> UsuarioPerfis { get; set; } = new List<UsuarioPerfil>();
        public ICollection<PerfilPermissao> PerfilPermissoes { get; set; } = new List<PerfilPermissao>();
    }

    public class Permissao
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string Nome { get; set; } = string.Empty;
        [MaxLength(200)] public string? Descricao { get; set; } = string.Empty;

        public ICollection<PerfilPermissao> PerfilPermissoes { get; set; } = new List<PerfilPermissao>();
    }

    public class UsuarioPerfil
    {
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public int PerfilId { get; set; }
        public Perfil Perfil { get; set; } = null!;
    }

    public class PerfilPermissao
    {
        public int PerfilId { get; set; }
        public Perfil Perfil { get; set; } = null!;

        public int PermissaoId { get; set; }
        public Permissao Permissao { get; set; } = null!;
    }

    public class PoliticaAcesso
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Nome { get; set; } = string.Empty;
        [MaxLength(500)] public string? Descricao { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
    }

    public class LogAuditoria
    {
        [Key] public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
        [MaxLength(100)] public string Acao { get; set; } = string.Empty;
        public string? Detalhe { get; set; } = string.Empty;
        [MaxLength(50)] public string Tipo { get; set; } = string.Empty;
        [MaxLength(50)] public string Origem { get; set; } = string.Empty;
    }

    public class ChamadoSuporte
    {
        [Key] public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        [Required, MaxLength(100)] public string Assunto { get; set; } = string.Empty;
        public string? Descricao { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Categoria { get; set; } = "Acesso";
        [Required, MaxLength(20)] public string Prioridade { get; set; } = "Normal";
        [MaxLength(50)] public string Status { get; set; } = "Aberto";
        public int? ResponsavelId { get; set; }
        public Usuario? Responsavel { get; set; }
        public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
        public DateTime? PrazoSla { get; set; }
        public DateTime? DataResolucao { get; set; }
        public DateTime? DataFechamento { get; set; }
        public string? Solucao { get; set; } = string.Empty;
        
        // Anexos de Arquivos (Storage Local / S3)
        [MaxLength(500)]
        public string? AnexoUrl { get; set; }
        [MaxLength(255)]
        public string? AnexoNomeOriginal { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class ConfiguracaoSistema
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Chave { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string Valor { get; set; } = string.Empty;
    }
}

