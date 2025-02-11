namespace Supera_Monitor_Back.Models.Aluno {
    public class CreateReposicaoRequest {
        public int Aluno_Id { get; set; }

        public int? Source_Aula_Id { get; set; }
        public DateTime? Source_Data { get; set; }
        public int? Source_Turma_Id { get; set; }
        public int? Source_Professor_Id { get; set; }

        public int? Dest_Aula_Id { get; set; }
        public DateTime? Dest_Data { get; set; }
        public int? Dest_Turma_Id { get; set; }
        public int? Dest_Professor_Id { get; set; }
    }
}
