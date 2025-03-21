using Supera_Monitor_Back.Helpers;

namespace Supera_Monitor_Back.Entities.Views;

public partial class ProfessorList : BaseList {
    public int Id { get; set; }

    public int Account_Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Telefone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime DataInicio { get; set; }

    public string CorLegenda { get; set; } = null!;

    public DateTime? DataNascimento { get; set; }

    public int? Professor_NivelCertificacao_Id { get; set; }

    public string? Professor_NivelCertificacao { get; set; }

    // 365.25 para considerar anos bissextos
    public int Idade => DataNascimento.HasValue
        ? ( int )((DateTime.Today - DataNascimento.Value).TotalDays / 365.25)
        : 0;

    public bool Aniversario => DataNascimento.HasValue
        && DataNascimento.Value.Day == TimeFunctions.HoraAtualBR().Day
        && DataNascimento.Value.Month == TimeFunctions.HoraAtualBR().Month;
}
