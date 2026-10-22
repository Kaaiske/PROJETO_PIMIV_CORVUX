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

        // Chave fornecida pelo cliente para impedir duplicidade em retries offline.
        [MaxLength(100)]
        public string? ClientIdempotencyKey { get; set; }
    }

    public class ConfiguracaoSistema
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Chave { get; set; } = string.Empty;
        [Required] public string Valor { get; set; } = string.Empty;
    }

    public class FirewallRegra
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string Ip { get; set; } = string.Empty;
        [Required, MaxLength(200)] public string Motivo { get; set; } = string.Empty;
        [MaxLength(10)] public string PaisOrigem { get; set; } = "BR";
        public DateTime DataBloqueio { get; set; } = DateTime.UtcNow;
        public bool Ativo { get; set; } = true;
        [MaxLength(100)] public string? CriadoPor { get; set; } = "SecOps AI";
    }

    public class DispositivoRede
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Hostname { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Ip { get; set; } = string.Empty;
        [MaxLength(50)] public string Vlan { get; set; } = "VLAN 10 - Corporativa";
        [MaxLength(100)] public string UsuarioResponsavel { get; set; } = string.Empty;
        [MaxLength(100)] public string SistemaOperacional { get; set; } = string.Empty;
        [MaxLength(50)] public string Tipo { get; set; } = "estacoes"; // estacoes, servidores
        public int CpuUsoPercentual { get; set; } = 15;
        public int RamUsoPercentual { get; set; } = 45;
        public int DiscoUsoPercentual { get; set; } = 35;
        public bool EdrAtivo { get; set; } = true;
        [MaxLength(100)] public string EdrStatus { get; set; } = "EDR Protegido";
        public int PatchesPendentes { get; set; } = 0;
        [MaxLength(50)] public string Status { get; set; } = "Saudavel"; // Saudavel, Alerta, Critico
        public bool IsoladoRede { get; set; } = false;
        public DateTime? UltimoScan { get; set; } = DateTime.UtcNow;
        public DateTime UltimoHeartbeat { get; set; } = DateTime.UtcNow;
    }

    public class NotaFiscal
    {
        [Key] public int Id { get; set; }
        public int Numero { get; set; }
        [Required, MaxLength(150)] public string TomadorNome { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string TomadorCnpj { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")] public decimal Valor { get; set; }
        [MaxLength(500)] public string Descricao { get; set; } = string.Empty;
        public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
        [MaxLength(50)] public string ProtocoloSefaz { get; set; } = string.Empty;
        [MaxLength(20)] public string Status { get; set; } = "Autorizada"; // Autorizada, Cancelada
        public int? EmitidoPorUsuarioId { get; set; }
        public Usuario? EmitidoPorUsuario { get; set; }
    }

    public class FechamentoMensal
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(20)] public string MesAno { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")] public decimal ReceitaBruta { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DespesasOperacionais { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal LucroLiquido { get; set; }
        public DateTime DataFechamento { get; set; } = DateTime.UtcNow;
        [MaxLength(20)] public string Status { get; set; } = "Fechado";
        public int? FechadoPorUsuarioId { get; set; }
        public Usuario? FechadoPorUsuario { get; set; }
    }

    public class RegistroFerias
    {
        [Key] public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        [Required, MaxLength(100)] public string ColaboradorNome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int Dias { get; set; } = 30;
        [MaxLength(30)] public string Status { get; set; } = "Aprovada"; // Agendada, Aprovada, Concluida
        public DateTime DataAprovacao { get; set; } = DateTime.UtcNow;
    }

    public class FolhaPagamento
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(20)] public string MesReferencia { get; set; } = string.Empty;
        public int TotalColaboradores { get; set; } = 18;
        [Column(TypeName = "decimal(18,2)")] public decimal ValorBrutoTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal ValorLiquidoTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal InssTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal FgtsTotal { get; set; }
        public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;
        [MaxLength(30)] public string Status { get; set; } = "Processada";
    }
}

