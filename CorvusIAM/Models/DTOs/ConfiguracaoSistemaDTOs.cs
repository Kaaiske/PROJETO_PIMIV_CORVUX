namespace CorvusIAM.Models.DTOs
{
    /// <summary>Parâmetro de configuração do sistema.</summary>
    public class ConfiguracaoResponseDTO
    {
        /// <summary>ID da configuração.</summary>
        public int Id { get; set; }
        /// <summary>Chave de identificação única do parâmetro.</summary>
        public string Chave { get; set; } = string.Empty;
        /// <summary>Valor atribuído ao parâmetro.</summary>
        public string Valor { get; set; } = string.Empty;
    }

    /// <summary>Modelo de criação de novo parâmetro de configuração.</summary>
    public class ConfiguracaoCreateDTO
    {
        /// <summary>Chave identificadora (ex: RETENCAO_LOGS_DIAS).</summary>
        public string Chave { get; set; } = string.Empty;
        /// <summary>Valor a ser armazenado.</summary>
        public string Valor { get; set; } = string.Empty;
    }

    /// <summary>Modelo de atualização de parâmetro de configuração.</summary>
    public class ConfiguracaoUpdateDTO
    {
        /// <summary>ID do parâmetro.</summary>
        public int Id { get; set; }
        /// <summary>Chave de identificação.</summary>
        public string Chave { get; set; } = string.Empty;
        /// <summary>Novo valor configurado.</summary>
        public string Valor { get; set; } = string.Empty;
    }
}
