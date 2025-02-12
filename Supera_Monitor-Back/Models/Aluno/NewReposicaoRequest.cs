namespace Supera_Monitor_Back.Models.Aluno {
    public class NewReposicaoRequest {
        public int Aluno_Id { get; set; }

        public int Source_Aula_Id { get; set; }
        public int Dest_Aula_Id { get; set; }
    }
}
