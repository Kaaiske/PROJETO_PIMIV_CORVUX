using System;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Models.DTOs
{
    /// <summary>
    /// Dados consolidados de resposta de um colaborador.
    /// </summary>
    public class UsuarioResponseDTO
    {
        /// <summary>Identificador exclusivo do usuário.</summary>
        public int Id { get; set; }
        /// <summary>Nome completo do colaborador.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Endereço de e-mail corporativo.</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>Status de ativação da conta (true: ativo, false: bloqueado).</summary>
        public bool Ativo { get; set; }
        /// <summary>Cadastro de Pessoa Física (CPF).</summary>
        public string? CPF { get; set; } = string.Empty;
        /// <summary>Data de nascimento.</summary>
        public DateTime? DataNascimento { get; set; }
        /// <summary>Cargo corporativo ocupado.</summary>
        public string? Cargo { get; set; } = string.Empty;
        /// <summary>Data de admissão / contratação.</summary>
        public DateTime? DataContratacao { get; set; }
        /// <summary>Registro Geral (RG).</summary>
        public string? RG { get; set; } = string.Empty;
        /// <summary>Telefone comercial ou móvel de contato.</summary>
        public string? Telefone { get; set; } = string.Empty;
        /// <summary>Departamento de alocação (ex: TI, Financeiro, RH, Operações).</summary>
        public string? Departamento { get; set; } = string.Empty;
        /// <summary>Endereço residencial cadastrado.</summary>
        public string? Endereco { get; set; } = string.Empty;
        /// <summary>ID do perfil principal atribuído ao colaborador.</summary>
        public int? PerfilId { get; set; }
    }

    /// <summary>
    /// Modelo para cadastro de um novo colaborador corporativo.
    /// </summary>
    public class UsuarioCreateDTO
    {
        /// <summary>Nome completo do novo colaborador.</summary>
        [Required, StringLength(100)]
        public string Nome { get; set; } = string.Empty;
        /// <summary>E-mail corporativo exclusivo.</summary>
        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        /// <summary>Senha inicial (mínimo 8 dígitos, maiúscula, minúscula, número e símbolo).</summary>
        [Required, MinLength(8)]
        public string Senha { get; set; } = string.Empty;
        /// <summary>Se a conta já nasce habilitada.</summary>
        public bool Ativo { get; set; } = true;
        /// <summary>Número do CPF.</summary>
        public string? CPF { get; set; } = string.Empty;
        /// <summary>Data de nascimento do colaborador.</summary>
        public DateTime? DataNascimento { get; set; }
        /// <summary>Cargo funcional.</summary>
        public string? Cargo { get; set; } = string.Empty;
        /// <summary>Data de contratação.</summary>
        public DateTime? DataContratacao { get; set; }
        /// <summary>Documento de RG.</summary>
        public string? RG { get; set; } = string.Empty;
        /// <summary>Telefone de contato.</summary>
        public string? Telefone { get; set; } = string.Empty;
        /// <summary>Departamento organizacional.</summary>
        public string? Departamento { get; set; } = string.Empty;
        /// <summary>Endereço residencial.</summary>
        public string? Endereco { get; set; } = string.Empty;
        /// <summary>ID do perfil de acesso (1: Admin, 2: TI, 3: Financeiro, 4: RH, 5: Operacional).</summary>
        public int? PerfilId { get; set; }
    }

    /// <summary>
    /// Modelo para atualização dos dados de um colaborador.
    /// </summary>
    public class UsuarioUpdateDTO
    {
        /// <summary>ID do colaborador.</summary>
        public int Id { get; set; }
        /// <summary>Nome completo.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>E-mail corporativo.</summary>
        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        /// <summary>Nova senha (deixar vazio se não desejar alterar).</summary>
        public string Senha { get; set; } = string.Empty;
        /// <summary>Status ativo/bloqueado.</summary>
        public bool Ativo { get; set; }
        /// <summary>CPF do colaborador.</summary>
        public string? CPF { get; set; } = string.Empty;
        /// <summary>Data de nascimento.</summary>
        public DateTime? DataNascimento { get; set; }
        /// <summary>Cargo funcional.</summary>
        public string? Cargo { get; set; } = string.Empty;
        /// <summary>Data de admissão.</summary>
        public DateTime? DataContratacao { get; set; }
        /// <summary>Documento de RG.</summary>
        public string? RG { get; set; } = string.Empty;
        /// <summary>Telefone de contato.</summary>
        public string? Telefone { get; set; } = string.Empty;
        /// <summary>Departamento corporativo.</summary>
        public string? Departamento { get; set; } = string.Empty;
        /// <summary>Endereço residencial.</summary>
        public string? Endereco { get; set; } = string.Empty;
        /// <summary>ID do perfil de acesso.</summary>
        public int? PerfilId { get; set; }
    }
}
