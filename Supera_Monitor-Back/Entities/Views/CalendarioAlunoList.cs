namespace Supera_Monitor_Back.Entities.Views {
    public class CalendarioAlunoList {
        public int Id { get; set; }

        public int Aluno_Id { get; set; }

        public int Aula_Id { get; set; }

        public string? Aluno { get; set; }

        public string? Aluno_Foto { get; set; }

        public int Turma_Id { get; set; }

        public string Turma { get; set; } = null!;

        public bool? Reposicao { get; set; }

        public bool? Presente { get; set; }

        public int? Apostila_Kit_Id { get; set; }

        public string? Kit { get; set; }

        public int? Apostila_Kit_Rel_Id { get; set; }

        public string? Apostila_Abaco { get; set; }

        public int? Apostila_Abaco_Id { get; set; }

        public int? NumeroPaginaAbaco { get; set; }

        public string? Apostila_AH { get; set; }

        public int? Apostila_AH_Id { get; set; }

        public int? NumeroPaginaAH { get; set; }

        public bool FlagAlunoNovo => false;
    }
}