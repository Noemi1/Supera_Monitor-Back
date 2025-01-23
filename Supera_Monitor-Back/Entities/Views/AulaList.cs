namespace Supera_Monitor_Back.Entities.Views {
    public partial class AulaList {
        public int Id { get; set; }

        public DateTime Data { get; set; }

        public int Professor_Id { get; set; }

        public int? Aluno_Id { get; set; }

        public string? Aluno_Nome { get; set; }

        public int Turma_Id { get; set; }

        public int? DiaSemana { get; set; }

        public TimeSpan? Horario { get; set; }

        public string? Turma_Tipo { get; set; }
    }
}
