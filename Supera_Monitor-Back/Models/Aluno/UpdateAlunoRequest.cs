namespace Supera_Monitor_Back.Models.Aluno {
    public class UpdateAlunoRequest {
        public int Id { get; set; }

        public string? Nome { get; set; }

        public DateTime? DataNascimento { get; set; }
    }
}
