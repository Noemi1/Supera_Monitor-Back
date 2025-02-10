namespace Supera_Monitor_Back.Entities.Views {
    public partial class CalendarioAlunoList {
        public int Id { get; set; }

        public int Aluno_Id { get; set; }

        public string? Aluno { get; set; }

        public string? Aluno_Foto { get; set; }

        public int Turma_Id { get; set; }

        public string Turma { get; set; } = null!;

        public bool? Resposicao { get; set; }

        public bool? Presente { get; set; }

        public int? ApostilaAbaco { get; set; }

        public int? NumeroPaginaAbaco { get; set; }

        public int? AH { get; set; }

        public int? NumeroPaginaAH { get; set; }
    }
}
