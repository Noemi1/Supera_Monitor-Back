using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Eventos
{

	public class Dashboard
	{
		public List<Dashboard_Roteiro> Roteiros { get; set; } = new List<Dashboard_Roteiro>();
		public List<Dashboard_Aluno> Alunos { get; set; } = new List<Dashboard_Aluno>();
	}


	public class Dashboard_Aula_Participacao
	{
		public bool Show { get; set; } = false;
		public Dashboard_Aula Aula { get; set; } = null!;
		public Dashboard_Participacao Participacao { get; set; } = null!;
	}

	// Pucas propriedades para ajudar no carregamento da API

	public class Dashboard_Aluno
	{
		public int Id { get; set; }

		public string? Nome { get; set; }

		public int? Turma_Id { get; set; }

		public string? Turma { get; set; }

		public string? CorLegenda { get; set; }

		public int? Checklist_Id { get; set; }

		public int? PrimeiraAula_Id { get; set; }

		public int? AulaZero_Id { get; set; }

		public DateTime? DataNascimento { get; set; } // Para exibir balão futuramente

		public string? Celular { get; set; }

		public List<Dashboard_Aula_Participacao> Aulas { get; set; } = new List<Dashboard_Aula_Participacao>();
	}

	public class Dashboard_Participacao
	{
		public int Id { get; set; }

		public int Aluno_Id { get; set; }

		public int Evento_Id { get; set; }

		public int? ReposicaoDe_Evento_Id { get; set; }

		public int? ReposicaoPara_Evento_Id { get; set; }

		public bool? Presente { get; set; }

		public string? Apostila_Abaco { get; set; }

		public string? Apostila_AH { get; set; }

		public int? Apostila_Abaco_Id { get; set; }

		public int? Apostila_AH_Id { get; set; }

		public int? NumeroPaginaAbaco { get; set; }

		public int? NumeroPaginaAH { get; set; }

		public string? Observacao { get; set; }

		public DateTime? Deactivated { get; set; }
	}

	public class Dashboard_Aula
	{
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


		public int? Sala_Id { get; set; }

		public int? Andar { get; set; }

		public int? NumeroSala { get; set; }


		public int? Roteiro_Id { get; set; }

		public string? Tema { get; set; }

		public int? Semana { get; set; }


		public int? Turma_Id { get; set; }
		
		public string? Turma { get; set; }

		public int CapacidadeMaximaAlunos { get; set; }


		public int? Professor_Id { get; set; }

		public string? Professor { get; set; }

		public string? CorLegenda { get; set; }

	}

	public class Dashboard_Roteiro
	{
		public int Id { get; set; }

		public string Tema { get; set; } = null!;

		public int Semana { get; set; }

		public DateTime DataInicio { get; set; }

		public DateTime DataFim { get; set; }

		public string? CorLegenda { get; set; }

	}
}