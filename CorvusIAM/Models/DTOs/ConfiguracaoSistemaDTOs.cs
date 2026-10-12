namespace CorvusIAM.Models.DTOs
{
    public class ConfiguracaoResponseDTO
    {
        public int Id { get; set; }
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    public class ConfiguracaoCreateDTO
    {
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    public class ConfiguracaoUpdateDTO
    {
        public int Id { get; set; }
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }
}
