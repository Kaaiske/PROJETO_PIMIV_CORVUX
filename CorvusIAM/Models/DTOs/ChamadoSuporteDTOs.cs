using System;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Models.DTOs
{
    public class ChamadoResponseDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string? Descricao { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Prioridade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? ResponsavelId { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime DataAtualizacao { get; set; }
        public DateTime? PrazoSla { get; set; }
        public DateTime? DataResolucao { get; set; }
        public DateTime? DataFechamento { get; set; }
        public string? Solucao { get; set; } = string.Empty;
    }

    public class ChamadoCreateDTO
    {
        [Required(ErrorMessage = "O assunto é obrigatório."), StringLength(100)]
        public string Assunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória."), StringLength(1000)]
        public string Descricao { get; set; } = string.Empty;

        private string _categoria = "Acesso";
        [StringLength(50)]
        public string Categoria 
        { 
            get => string.IsNullOrWhiteSpace(_categoria) ? "Acesso" : _categoria; 
            set => _categoria = string.IsNullOrWhiteSpace(value) ? "Acesso" : value; 
        }

        private string _prioridade = "Normal";
        public string Prioridade 
        { 
            get => string.IsNullOrWhiteSpace(_prioridade) ? "Normal" : _prioridade; 
            set => _prioridade = string.IsNullOrWhiteSpace(value) ? "Normal" : value; 
        }
    }

    public class ChamadoUpdateDTO
    {
        [Required]
        public int Id { get; set; }
        
        [StringLength(100)]
        public string Assunto { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Descricao { get; set; } = string.Empty;
        [StringLength(50)]
        public string Categoria { get; set; } = string.Empty;
        [RegularExpression("^(Baixa|Normal|Alta|Critica)$")]
        public string Prioridade { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
        public int? ResponsavelId { get; set; }
        public string? Solucao { get; set; } = string.Empty;
        
        public DateTime? DataFechamento { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
