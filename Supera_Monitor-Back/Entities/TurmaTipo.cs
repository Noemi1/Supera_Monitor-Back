
namespace Supera_Monitor_Back.Entities {
    public partial class TurmaTipo {
        public int Id { get; set; }

        public string? Nome { get; set; }

        public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
    }

    public enum Tipo {
        A = 1,
        B = 2,
    }
}
