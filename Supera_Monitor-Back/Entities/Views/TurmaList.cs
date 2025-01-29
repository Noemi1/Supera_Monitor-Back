namespace Supera_Monitor_Back.Entities.Views {
    public partial class TurmaList : BaseList {
        public int Id { get; set; }

        public int DiaSemana { get; set; }

        public TimeSpan? Horario { get; set; }

        public int? Professor_Id { get; set; }

        public string Nome_Professor { get; set; } = null!;

        public string Email_Professor { get; set; } = null!;

        public string Telefone_Professor { get; set; } = null!;

        public int? NivelAbaco { get; set; }

        public int? NivelAH { get; set; }

        public int? Turma_Tipo_Id { get; set; }

        public string? Turma_Tipo { get; set; }
    }
}