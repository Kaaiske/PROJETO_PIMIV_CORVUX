namespace CorvusIAM.Models.DTOs
{
    public class PoliticaResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; } = string.Empty;
        public bool Ativa { get; set; }
    }

    public class PoliticaCreateDTO
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
    }

    public class PoliticaUpdateDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; } = string.Empty;
        public bool Ativa { get; set; }
    }
}
