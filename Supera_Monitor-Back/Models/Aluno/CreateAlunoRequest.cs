namespace Supera_Monitor_Back.Models.Aluno {
    public class CreateAlunoRequest {
        public int Pessoa_Id { get; set; }
        public int Turma_Id { get; set; }
        public string? Aluno_Foto { get; set; }

        public int AspNetUsers_Created_Id { get; set; }
    }
}
