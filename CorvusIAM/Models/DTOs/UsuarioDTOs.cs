using System;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Models.DTOs
{
    public class UsuarioResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public string CPF { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public DateTime? DataContratacao { get; set; }
        public string RG { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public int? PerfilId { get; set; }
    }

    public class UsuarioCreateDTO
    {
        [Required, StringLength(100)]
        public string Nome { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)]
        public string Senha { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public string CPF { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public DateTime? DataContratacao { get; set; }
        public string RG { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public int? PerfilId { get; set; }
    }

    public class UsuarioUpdateDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public string CPF { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public DateTime? DataContratacao { get; set; }
        public string RG { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public int? PerfilId { get; set; }
    }
}
