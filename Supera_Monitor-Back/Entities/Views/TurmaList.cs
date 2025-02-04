namespace Supera_Monitor_Back.Entities.Views {
    public partial class TurmaList : BaseList {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int DiaSemana { get; set; }

        public TimeSpan? Horario { get; set; }

        public int CapacidadeMaximaAlunos { get; set; }

        public int? Unidade_Id { get; set; }

        public int? Professor_Id { get; set; }

        public string? Professor { get; set; } = null!;

        public int? Turma_Tipo_Id { get; set; }

        public string? Turma_Tipo { get; set; }
    }
}