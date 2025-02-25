namespace Supera_Monitor_Back.Models.Turma {
    public class UpdateTurmaRequest {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public int DiaSemana { get; set; }
        public TimeSpan? Horario { get; set; }
        public int CapacidadeMaximaAlunos { get; set; }

        public int Unidade_Id { get; set; }

        public int? Sala_Id { get; set; }
        public int? Professor_Id { get; set; }

        public List<PerfilCognitivoModel> PerfilCognitivo { get; set; } = new List<PerfilCognitivoModel>();
    }
}
