namespace CorvusIAM.Models.DTOs
{
    /// <summary>Política de segurança ou regra de acesso condicional (ABAC).</summary>
    public class PoliticaResponseDTO
    {
        /// <summary>ID numérico da política.</summary>
        public int Id { get; set; }
        /// <summary>Nome da política.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Descrição da regra (ex: Exigir MFA para acessos remotos fora do horário comercial).</summary>
        public string? Descricao { get; set; } = string.Empty;
        /// <summary>Se a política está ativa ou suspensa.</summary>
        public bool Ativa { get; set; }
    }

    /// <summary>Modelo para criação de nova política.</summary>
    public class PoliticaCreateDTO
    {
        /// <summary>Nome da política.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Descrição da regra.</summary>
        public string? Descricao { get; set; } = string.Empty;
        /// <summary>Status ativo (padrão true).</summary>
        public bool Ativa { get; set; } = true;
    }

    /// <summary>Modelo para alteração de política existente.</summary>
    public class PoliticaUpdateDTO
    {
        /// <summary>ID da política.</summary>
        public int Id { get; set; }
        /// <summary>Nome da política.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Descrição da regra.</summary>
        public string? Descricao { get; set; } = string.Empty;
        /// <summary>Se está ativa.</summary>
        public bool Ativa { get; set; }
    }
}
