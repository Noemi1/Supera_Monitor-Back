using Supera_Monitor_Back.Models.Eventos.Aula;

namespace Supera_Monitor_Back.Models.Eventos.Dtos;

public class CreateEventDto {
    public DateTime Data { get; set; }
    public string? Descricao { get; set; }
    public string? Observacao { get; set; }
    public int DuracaoMinutos { get; set; }
    public int CapacidadeMaximaAlunos { get; set; }

    public int Evento_Tipo_Id { get; set; }
    public int Sala_Id { get; set; }

    public int? Turma_Id { get; set; }
    public int? Roteiro_Id { get; set; }

    public List<int> Alunos { get; set; } = [];
    public List<int> Professores { get; set; } = [];
    public List<int>? PerfilCognitivo { get; set; } = null;
}

public class CreateClassEventDto : CreateEventDto {
    public int? ReagendamentoDe_Evento_Id { get; set; }
    public List<ReposicaoAlunoModel> Reposicoes { get; set; } = [];
}