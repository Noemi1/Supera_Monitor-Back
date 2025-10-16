namespace Supera_Monitor_Back.Models.Dashboard {
    public class Dashboard_Aula {
        public int Id { get; set; }

        public int Evento_Tipo_Id { get; set; }

        public string Evento_Tipo { get; set; } = null!;

        public DateTime Data { get; set; }

        public string Descricao { get; set; } = null!;

        public string? Observacao { get; set; }

        public int DuracaoMinutos { get; set; }

        public bool Finalizado { get; set; }


        public bool Active => Deactivated == null;

        public int? Account_Created_Id { get; set; }

        public string? Account_Created { get; set; }

        public DateTime Created { get; set; }

        public DateTime? LastUpdated { get; set; }

        public DateTime? Deactivated { get; set; }

        public int? ReagendamentoDe_Evento_Id { get; set; }

        public int? ReagendamentoPara_Evento_Id { get; set; }


        public string? Sala { get; set; }

        public int? Sala_Id { get; set; }

        public int? Andar { get; set; }

        public int? NumeroSala { get; set; }


        public int? Roteiro_Id { get; set; }

        public string? Tema { get; set; }

        public int? Semana { get; set; }


        public int? Turma_Id { get; set; }

        public string? Turma { get; set; }

        public int CapacidadeMaximaAlunos { get; set; }

        public int AlunosAtivos { get; set; }

        public int VagasDisponiveis { get; set; }


        public int? Professor_Id { get; set; }

        public string? Professor { get; set; }

        public string? CorLegenda { get; set; }

    }
}
