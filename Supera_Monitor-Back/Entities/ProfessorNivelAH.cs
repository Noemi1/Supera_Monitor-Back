namespace Supera_Monitor_Back.Entities {
    public partial class Professor_NivelAH {
        public int Id { get; set; }

        public string Descricao { get; set; } = null!;

        public virtual ICollection<Professor> Professors { get; set; } = new List<Professor>();
    }

}
