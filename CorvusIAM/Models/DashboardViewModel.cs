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
    }
}
