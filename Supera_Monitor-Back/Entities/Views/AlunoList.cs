namespace Supera_Monitor_Back.Entities.Views {
    public class AlunoList {
        public int Id { get; set; }

        public int Pessoa_Id { get; set; }

        public string? Nome { get; set; }

        public DateTime? DataNascimento { get; set; }

        public int Turma_Id { get; set; }

        public int DiaSemana { get; set; }

        public TimeSpan? Horario { get; set; }
    }
}
