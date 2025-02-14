namespace Supera_Monitor_Back.Models.Aula {
    public class UpdateRegistroRequest {
        public int Turma_Aula_Aluno_Id { get; set; }

        public bool Presente { get; set; }

        public int Apostila_Abaco_Id { get; set; }
        public int Numero_Pagina_Abaco { get; set; }

        public int Apostila_Ah_Id { get; set; }
        public int Numero_Pagina_Ah { get; set; }
    }
}
