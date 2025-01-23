namespace Supera_Monitor_Back.Models.Aluno {
    public class CreateAlunoRequest {
        public int? Pessoa_Id { get; set; }

        public string? Nome { get; set; } = null!;
        public DateTime? DataNascimento { get; set; }

        public int Turma_Id { get; set; }
    }
}
