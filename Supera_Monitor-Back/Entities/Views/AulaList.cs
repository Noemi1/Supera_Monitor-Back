namespace Supera_Monitor_Back.Entities.Views {
    public partial class AulaList {
        public int Id { get; set; }

        public int? Turma_Id { get; set; }

        public DateTime Data { get; set; }

        public int DiaSemana { get; set; }

        public TimeSpan? Horario { get; set; }

        public int Turma_Tipo_Id { get; set; }

        public string? Turma_Tipo { get; set; }

        public int Professor_Id { get; set; }

        public int? NivelAH { get; set; }

        public int? NivelAbaco { get; set; }

        public DateTime DataInicio { get; set; }
    }
}
