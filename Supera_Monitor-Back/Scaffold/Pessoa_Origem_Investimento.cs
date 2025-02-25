using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Pessoa_Origem_Investimento
{
    public int Id { get; set; }

    public int Unidade_Id { get; set; }

    public int? Pessoa_Origem_Id { get; set; }

    public int Mes { get; set; }

    public int Ano { get; set; }

    public decimal? Fee { get; set; }

    public decimal? Investimento { get; set; }

    public decimal? InvestimentoOutrasMidias { get; set; }

    public decimal? InvestimentoEquipeComercial { get; set; }

    public decimal? OutrosInvestimentos { get; set; }
}
