namespace Supera_Monitor_Back.Models.Aluno {
    public class AlunoResponse {
        public int Id { get; set; }

        public string Nome { get; set; } = null!;
        public int Pessoa_Id { get; set; }

        public int Turma_Id { get; set; }

        public DateTime? DataNascimento { get; set; }
    }
}
