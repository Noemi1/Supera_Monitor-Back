using Supera_Monitor_Back.Models;

namespace Supera_Monitor_Back.Entities.Views;

public partial class TurmaList {
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public int DiaSemana { get; set; }
    public TimeSpan? Horario { get; set; }
    public int CapacidadeMaximaAlunos { get; set; }
    public int AlunosAtivos { get; set; }
    public int? Unidade_Id { get; set; }
    public string? LinkGrupo { get; set; }
    public int Account_Created_Id { get; set; }
    public string Account_Created { get; set; } = null!;
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public bool Active => !Deactivated.HasValue;
    public int? Professor_Id { get; set; }
    public string? Professor { get; set; }
    public string? CorLegenda { get; set; }
    public int? Sala_Id { get; set; }
    public int? Andar { get; set; }
    public int? NumeroSala { get; set; }
    public List<PerfilCognitivoModel> PerfilCognitivo { get; set; } = new();
}

