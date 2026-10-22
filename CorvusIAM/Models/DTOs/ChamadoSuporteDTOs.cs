using System;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Models.DTOs
{
    /// <summary>
    /// Representação dos dados de um chamado técnico do Service Desk.
    /// </summary>
    public class ChamadoResponseDTO
    {
        /// <summary>Identificador único do chamado.</summary>
        public int Id { get; set; }
        /// <summary>Identificador do colaborador que abriu o chamado.</summary>
        public int UsuarioId { get; set; }
        /// <summary>Assunto ou resumo do problema/solicitação.</summary>
        public string Assunto { get; set; } = string.Empty;
        /// <summary>Descrição detalhada da solicitação ou incidente.</summary>
        public string? Descricao { get; set; } = string.Empty;
        /// <summary>Categoria da solicitação (ex: Acesso, Hardware, Software, Rede, Segurança).</summary>
        public string Categoria { get; set; } = string.Empty;
        /// <summary>Prioridade do chamado (Baixa, Normal, Alta, Critica).</summary>
        public string Prioridade { get; set; } = string.Empty;
        /// <summary>Status atual (Aberto, Em_Andamento, Resolvido, Fechado).</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>Identificador do analista responsável pelo atendimento.</summary>
        public int? ResponsavelId { get; set; }
        /// <summary>Data e hora de abertura do chamado (UTC).</summary>
        public DateTime DataAbertura { get; set; }
        /// <summary>Data e hora da última atualização no chamado.</summary>
        public DateTime DataAtualizacao { get; set; }
        /// <summary>Prazo máximo para atendimento e resolução (SLA).</summary>
        public DateTime? PrazoSla { get; set; }
        /// <summary>Data e hora em que a solução foi aplicada.</summary>
        public DateTime? DataResolucao { get; set; }
        /// <summary>Data e hora de encerramento definitivo.</summary>
        public DateTime? DataFechamento { get; set; }
        /// <summary>Descrição técnica da solução aplicada pelo suporte.</summary>
        public string? Solucao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modelo para abertura de novos chamados no Service Desk.
    /// </summary>
    public class ChamadoCreateDTO
    {
        /// <summary>Chave opcional para tornar retries da mesma solicitação idempotentes.</summary>
        [StringLength(100)]
        public string? ClientIdempotencyKey { get; set; }

        /// <summary>Título breve da solicitação (até 100 caracteres).</summary>
        [Required(ErrorMessage = "O assunto é obrigatório."), StringLength(100)]
        public string Assunto { get; set; } = string.Empty;

        /// <summary>Detalhamento do incidente, necessidade de acesso ou suporte.</summary>
        [Required(ErrorMessage = "A descrição é obrigatória."), StringLength(1000)]
        public string Descricao { get; set; } = string.Empty;

        private string _categoria = "Acesso";
        /// <summary>Categoria do chamado (padrão: Acesso).</summary>
        [StringLength(50)]
        public string Categoria 
        { 
            get => string.IsNullOrWhiteSpace(_categoria) ? "Acesso" : _categoria; 
            set => _categoria = string.IsNullOrWhiteSpace(value) ? "Acesso" : value; 
        }

        private string _prioridade = "Normal";
        /// <summary>Grau de criticidade: Baixa, Normal, Alta ou Critica (padrão: Normal).</summary>
        public string Prioridade 
        { 
            get => string.IsNullOrWhiteSpace(_prioridade) ? "Normal" : _prioridade; 
            set => _prioridade = string.IsNullOrWhiteSpace(value) ? "Normal" : value; 
        }
    }

    /// <summary>
    /// Modelo para atualização de dados de um chamado existente.
    /// </summary>
    public class ChamadoUpdateDTO
    {
        /// <summary>Identificador do chamado a ser atualizado.</summary>
        [Required]
        public int Id { get; set; }
        
        /// <summary>Assunto do chamado.</summary>
        [StringLength(100)]
        public string Assunto { get; set; } = string.Empty;
        
        /// <summary>Descrição do chamado.</summary>
        [StringLength(1000)]
        public string? Descricao { get; set; } = string.Empty;
        
        /// <summary>Categoria do chamado.</summary>
        [StringLength(50)]
        public string Categoria { get; set; } = string.Empty;
        
        /// <summary>Prioridade do chamado.</summary>
        [RegularExpression("^(Baixa|Normal|Alta|Critica)$")]
        public string Prioridade { get; set; } = string.Empty;
        
        /// <summary>Status de atendimento.</summary>
        [RegularExpression("^(Aberto|Em_Andamento|Resolvido|Fechado)$")]
        public string Status { get; set; } = string.Empty;
        
        /// <summary>ID do técnico responsável atribuído.</summary>
        public int? ResponsavelId { get; set; }
        
        /// <summary>Resolução aplicada.</summary>
        [StringLength(1000)]
        public string? Solucao { get; set; } = string.Empty;
    }
}
