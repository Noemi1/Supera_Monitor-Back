using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Sala;

namespace Supera_Monitor_Back.Models.Eventos;

public class CalendarioResponse {
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public string Descricao { get; set; } = null!;

    public string? Observacao { get; set; }

    public int Evento_Tipo_Id { get; set; }

    public string Evento_Tipo { get; set; } = null!;

    public bool Finalizado { get; set; }

    public int Account_Created_Id { get; set; }

    public SalaModel? Sala { get; set; } = null!;

    public EventoAulaModel? Aula { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public int? ReagendamentoDe_Evento_Id { get; set; }
}

//tipo_Id: EventoTipo = EventoTipo.aula;
//descricao: string = '';
//observacao: string = '';
//finalizado: boolean = false;

//reagendamentoDe_Evento_Id?: number;
//reagendamentoDe_Evento?: Evento;

//alunos: Evento_Participacao_Aluno[] = [];
//professores: Evento_Professor_Participacao[] = [];

//// Aula
//professor_Id?: number;
//professor?: string;
//corLegenda?: string;
//turma_Id?: number;
//turma?: string;
//perfilCognitivo: PerfilCognitivo[] = [];
//capacidadeMaximaAlunos: number = 0;

//created: Date = new Date;
//deactivated?: Date;
//active: boolean = true;