namespace Supera_Monitor_Back.Entities.Views {
    public partial class AulaList {
        public int Id { get; set; }

        public int Turma_Id { get; set; }

        public string Turma { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        public int Professor_Id { get; set; }

        public string Professor { get; set; } = null!;
    }
}
