using System;
using System.Collections.Generic;
using CorvusIAM.Entities;

namespace CorvusIAM.Models
{
    public class RHViewModel
    {
        public int TotalColaboradoresAtivos { get; set; }
        public decimal TotalFolhaSalarial { get; set; }
        public int FeriasAVencerAlertas { get; set; }
        public string StatusControlePonto { get; set; } = "100% Regular";
        public List<FolhaPagamento> Folhas { get; set; } = new List<FolhaPagamento>();
        public List<RegistroFerias> Ferias { get; set; } = new List<RegistroFerias>();
        public List<Usuario> Colaboradores { get; set; } = new List<Usuario>();
    }
}

