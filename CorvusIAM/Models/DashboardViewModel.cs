using System;
using System.Collections.Generic;
using CorvusIAM.Entities;

namespace CorvusIAM.Models
{
    public class DashboardViewModel
    {
        public int TotalUsuarios { get; set; }
        public int AcessosBloqueados { get; set; }
        public int DispositivosMobile { get; set; }
        public double UsoNuvem { get; set; }
        
        public List<LogAuditoria> LogsRecentes { get; set; } = new List<LogAuditoria>();
        
        public List<int> LoginsNormaisPorDia { get; set; } = new List<int> { 0,0,0,0,0,0,0 };
        public List<int> BloqueiosPorDia { get; set; } = new List<int> { 0,0,0,0,0,0,0 };
        public List<int> AcessosPlataforma { get; set; } = new List<int> { 0,0,0 };

        public List<ModuloStatus> Modulos { get; set; } = new List<ModuloStatus>();

        // Métricas Detalhadas de Uso por Usuário e Setor
        public List<UsuarioUsoMetrica> RankingUsoUsuarios { get; set; } = new List<UsuarioUsoMetrica>();
        public List<string> DepartamentosNomes { get; set; } = new List<string> { "Administração / Diretoria", "Segurança da Informação", "Recursos Humanos", "Contabilidade & Finanças", "Engenharia / DevOps", "HelpDesk / Operações" };
        public List<int> DepartamentosUsoPercentual { get; set; } = new List<int> { 32, 26, 16, 12, 9, 5 };
        public int TotalRequisicoesHoje { get; set; } = 4820;
        public int ConexoesAtivasRede { get; set; } = 18;
        public int IdsAmeacasBloqueadas { get; set; } = 14;
    }

    public class UsuarioUsoMetrica
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public int TotalLogins { get; set; }
        public int TotalRequisicoes { get; set; }
        public DateTime UltimoAcesso { get; set; }
        public string IpOrigem { get; set; } = string.Empty;
        public string Dispositivo { get; set; } = string.Empty;
        public string Status { get; set; } = "Ativo";
    }


    public class ModuloStatus
    {
        public string Nome { get; set; } = string.Empty;
        public string Versao { get; set; } = string.Empty;
        public string Latencia { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // ONLINE, OFFLINE, LENTO
        public string Icone { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public string Detalhe { get; set; } = string.Empty;
        public string? Tipo { get; set; } = "API / Webhook";
        public string? EndpointUrl { get; set; } = string.Empty;
        public string? ApiKey { get; set; } = string.Empty;
    }
}
