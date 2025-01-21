namespace Supera_Monitor_Back.Entities {
    public class Turma_Tipo {

        public Turma_Tipo()
        {
            Turma = new HashSet<Turma>();
        }

        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public virtual ICollection<Turma> Turma { get; set; }
    }

    public enum Tipo {
        A = 1,
        B = 2,
    }
}
