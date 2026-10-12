using System;
using System.ComponentModel.DataAnnotations;

namespace CorvusIAM.Entities
{
    public class Integracao
    {
        [Key]
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty; // e.g. "ph-google-logo"
        public string Detalhe { get; set; } = string.Empty; // e.g. "SSO Integrado"
        public bool Ativo { get; set; } = true;
        public DateTime DataConexao { get; set; } = DateTime.Now;
        public string ApiKey { get; set; } = string.Empty;
    }
}
