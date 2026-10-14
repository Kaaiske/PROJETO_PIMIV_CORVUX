using System.Collections.Generic;

namespace CorvusIAM.Models.DTOs
{
    /// <summary>Perfil de acesso corporativo (RBAC).</summary>
    public class PerfilResponseDTO
    {
        /// <summary>ID numérico do perfil.</summary>
        public int Id { get; set; }
        /// <summary>Nome do perfil (ex: Administrador, TI Suporte, RH).</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Descrição e finalidade das atribuições do perfil.</summary>
        public string? Descricao { get; set; } = string.Empty;
    }

    /// <summary>Modelo de criação de novo perfil.</summary>
    public class PerfilCreateDTO
    {
        /// <summary>Nome do novo perfil.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Descrição das funções do perfil.</summary>
        public string? Descricao { get; set; } = string.Empty;
    }

    /// <summary>Modelo de atualização de perfil existente.</summary>
    public class PerfilUpdateDTO
    {
        /// <summary>ID do perfil a ser modificado.</summary>
        public int Id { get; set; }
        /// <summary>Nome atualizado do perfil.</summary>
        public string Nome { get; set; } = string.Empty;
        /// <summary>Descrição atualizada.</summary>
        public string? Descricao { get; set; } = string.Empty;
    }
}
