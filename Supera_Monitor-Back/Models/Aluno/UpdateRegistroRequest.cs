namespace Supera_Monitor_Back.Models.Aluno {
    public class UpdateRegistroRequest {
        public int Turma_Aula_Aluno_Id { get; set; }

        public bool Presente { get; set; }

        public int Numero_Pagina_Ah { get; set; }
        public int Numero_Pagina_Abaco { get; set; }
    }
}
