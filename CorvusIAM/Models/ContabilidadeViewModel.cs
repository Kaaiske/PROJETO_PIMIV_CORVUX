using System;
using System.Collections.Generic;
using CorvusIAM.Entities;

namespace CorvusIAM.Models
{
    public class ContabilidadeViewModel
    {
        public decimal TotalFaturamento { get; set; }
        public int TotalNotasEmitidas { get; set; }
        public decimal ContasAPagarSemana { get; set; }
        public string StatusConciliacao { get; set; } = "Em Dia";
        public List<NotaFiscal> NotasFiscais { get; set; } = new List<NotaFiscal>();
        public List<FechamentoMensal> Fechamentos { get; set; } = new List<FechamentoMensal>();
    }
}

