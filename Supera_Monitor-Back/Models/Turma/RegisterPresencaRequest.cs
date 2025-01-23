namespace Supera_Monitor_Back.Models.Turma {
    public class RegisterPresencaRequest {
        public int Turma_Aula_Id { get; set; }
        public int Aluno_Id { get; set; }

        public bool? Presente { get; set; }
        public int? NumeroPaginaAbaco { get; set; }
        public int? NumeroPaginaAH { get; set; }
        public int? ApostilaAbaco { get; set; }
        public int? Ah { get; set; }
    }
}
