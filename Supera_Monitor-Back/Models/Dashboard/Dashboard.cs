using System.ComponentModel.DataAnnotations;
using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Models.Dashboard
{
    public class Dashboard
    {
        public List<DashboardAluno> Alunos { get; set; } = new List<DashboardAluno>();
        public List<DashboardMesesRoteiro> MesesRoteiro { get; set; } = new List<DashboardMesesRoteiro>();
    }
	public class DashboardMesesRoteiro
	{

        public int Mes { get; set; }
        public string MesString { get; set; } = string.Empty;
        public List<DashboardRoteiro> Roteiros { get; set; } = new List<DashboardRoteiro>();
	}

    public class DashboardRoteiro
    {
        public int Id { get; set; }
        public string Tema { get; set; } = string.Empty;
        public int Semana { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string CorLegenda { get; set; } = string.Empty;
        public bool Recesso { get; set; }
    }

    public class DashboardAluno
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Celular { get; set; }

        public int? Checklist_Id { get; set; }
        public int? PrimeiraAula_Id { get; set; }
        public int? AulaZero_Id { get; set; }

        public DateTime? DataNascimento { get; set; }

        public int PerfilCognitivo_Id { get; set; }
        public string? CorLegenda { get; set; }
		public string? Turma { get; set; }
        public int? Turma_Id { get; set; }

        public virtual List<DashboardAlunoAulaReposicao> Items { get; set; } = new List<DashboardAlunoAulaReposicao>();
    }

    public class DashboardAlunoAulaReposicao
    {
        public int Id { get; set; }

        public bool Show { get; set; }

        public virtual DashboardAulaParticipacao Aula { get; set; } = null!;

		public virtual DashboardAulaParticipacao? ReposicaoPara { get; set; }
	}

    public class DashboardAulaParticipacao
    {
		public virtual DashboardAula Aula { get; set; } = null!;
        public virtual DashboardParticipacao Participacao { get; set; } = null!;
	}

    public class DashboardAula
    {
        public int Id { get; set; }

        public EventoTipo EventoTipo_Id { get; set; }
        public DateTime Data { get; set; }
        public string Descricao { get; set; } = String.Empty;
        public string Observacao { get; set; } = String.Empty;
        public bool Finalizado { get; set; }
        public DateTime? Deactivated { get; set; }
		public bool Active => !Deactivated.HasValue;

		public string Sala { get; set; } = String.Empty;
		public int Andar { get; set; }
        public int NumeroSala { get; set; }

		public bool Recesso { get; set; }
        public string Tema { get; set; } = String.Empty;
		public int Semana { get; set; }
        public string RoteiroCorLegenda { get; set; } = String.Empty;


		public string Turma { get; set; } = String.Empty;
		public string Professor { get; set; } = String.Empty;
		public string CorLegenda { get; set; } = String.Empty;

		public virtual Feriado? Feriado { get; set; }
    }

    public class DashboardParticipacao
    {
        public int Id { get; set; }

        public bool? Presente { get; set; }
        public string Observacao { get; set; } = String.Empty;
		public DateTime? Deactivated { get; set; }
		public bool Active => !Deactivated.HasValue;
		public string ApostilaAbaco { get; set; } = String.Empty;
		public string ApostilaAH { get; set; } = String.Empty;
		public int? NumeroPaginaAbaco { get; set; }
        public int? NumeroPaginaAH { get; set; }

        public DateTime? AlunoContactado { get; set; }
        public StatusContato? StatusContato_Id { get; set; }
        public string? ContatoObservacao { get; set; } 
		public int? ReposicaoPara_Evento_Id { get; set; }
		public int? ReposicaoDe_Evento_Id { get; set; }
	}

    public class Feriado
    {
        public string Name { get; set; } = String.Empty;
		public DateTime Date { get; set; }
    }
}