using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Models.Aula {
    public class AulaResponse {
        public int Id { get; set; }
        public DateTime Data { get; set; }

        public int Professor_Id { get; set; }
        public string Professor { get; set; } = null!;

        public int? Turma_Id { get; set; }
        public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();
    }
}
