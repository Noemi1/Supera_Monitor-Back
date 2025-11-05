namespace Supera_Monitor_Back.Entities;

public partial class Turma {
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public int DiaSemana { get; set; }
    public int? Professor_Id { get; set; }
    public TimeSpan? Horario { get; set; }
    public int CapacidadeMaximaAlunos { get; set; }
    public int? Unidade_Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public int Account_Created_Id { get; set; }
    public int? Sala_Id { get; set; }
    public string? LinkGrupo { get; set; }
    public bool Active => !Deactivated.HasValue;
    public virtual Account Account_Created { get; set; } = null!;
    public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();
    public virtual ICollection<Evento_Aula> Evento_Aulas { get; set; } = new List<Evento_Aula>();
    public virtual Professor? Professor { get; set; }
    public virtual Sala? Sala { get; set; }
    public virtual ICollection<Aluno_Turma_Vigencia> Aluno_Turma_Vigencia { get; set; } = new List<Aluno_Turma_Vigencia>();
    public virtual ICollection<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rels { get; set; } = new List<Turma_PerfilCognitivo_Rel>();

    public bool PossuiVagas(int vagasOcupadas, int vagasRequisitadas) {
        int vagasDisponiveis = this.CapacidadeMaximaAlunos - vagasOcupadas;

        return (vagasDisponiveis - vagasRequisitadas) >= 0;
    }

    //public List<SequenciaVigencia> ExtrairSequenciaVigencias() {
    //    if (this.Alunos is null) {
    //        throw new Exception("Verifique se os alunos estão incluídos na turma.");
    //    }

    //    var inicioVigencias = this.Alunos
    //        .Where(a => a.Deactivated == null)
    //        .Select(a => new SequenciaVigencia { Tipo = VigenciaTipo.Inicio, Data = a.DataInicioVigencia });

    //    var fimVigencias = this.Alunos
    //        .Where(a => a.Deactivated == null)
    //        .Select(a => new SequenciaVigencia
    //        {
    //            Tipo = VigenciaTipo.Fim,
    //            Data = a.DataFimVigencia ?? a.DataInicioVigencia.AddYears(100)
    //        });

    //    List<SequenciaVigencia> sequenciaVigencias = inicioVigencias.Concat(fimVigencias).OrderBy(v => v.Data).ToList();

    //    return sequenciaVigencias;
    //}

    //public bool VerificarCompatibilidadeVigencia(Aluno novoAluno) {
    //    var turmaVigencias = this.ExtrairSequenciaVigencias();

    //    // Adicionar início e fim da vigência do novo aluno
    //    turmaVigencias.Add(new SequenciaVigencia { Tipo = VigenciaTipo.Inicio, Data = novoAluno.DataInicioVigencia });
    //    turmaVigencias.Add(new SequenciaVigencia { Tipo = VigenciaTipo.Fim, Data = novoAluno.DataFimVigencia ?? novoAluno.DataInicioVigencia.AddYears(100) });

    //    turmaVigencias = turmaVigencias.OrderBy(tv => tv.Data).ToList();

    //    // Verificar se em qualquer momento da vigência após a inclusão do novo aluno, o número de alunos ativos extrapola o limite da turma
    //    int alunosAtivos = 0;

    //    foreach (var vigencia in turmaVigencias) {
    //        if (vigencia.Tipo == VigenciaTipo.Inicio) {
    //            alunosAtivos++;
    //        }
    //        else {
    //            alunosAtivos--;
    //        }

    //        // Verificar se a capacidade excedida em algum ponto
    //        if (alunosAtivos > this.CapacidadeMaximaAlunos) {
    //            return false;
    //        }
    //    }

    //    return true;
    //}
}

//public enum VigenciaTipo {
//    Inicio = 1,
//    Fim = 2,
//}

//public class SequenciaVigencia {
//    public VigenciaTipo Tipo { get; set; }
//    public DateTime Data { get; set; }
//}
