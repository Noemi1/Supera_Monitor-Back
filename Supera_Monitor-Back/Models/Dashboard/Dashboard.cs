using System.ComponentModel.DataAnnotations;
using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Models.Dashboard
{
    public class Dashboard
    {
        public List<DashboardAluno> Alunos { get; set; } = [];
        public List<DashboardRoteiro> Roteiros { get; set; } = [];
    }

    public class DashboardRoteiro
    {
        public int Id { get; set; }
        public string Tema { get; set; }
        public int Semana { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string CorLegenda { get; set; }
        public bool Recesso { get; set; }
    }

    public class DashboardAluno
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Celular { get; set; }

        public int? ChecklistId { get; set; }
        public int? PrimeiraAulaId { get; set; }
        public int? AulaZeroId { get; set; }

        public DateTime? DataNascimento { get; set; }

        public int PerfilCognitivoId { get; set; }
        public string CorLegenda { get; set; }
        public string Turma { get; set; }
        public int TurmaId { get; set; }

        public virtual List<DashboardAlunoAula> Aulas { get; set; }
    }

    public class DashboardAlunoAula
    {
        public int Id { get; set; }

        public bool Show { get; set; }

        public virtual DashboardAulaParticipacao Aula { get; set; }
        public virtual DashboardAulaParticipacao ReposicaoPara { get; set; }
    }

    public class DashboardAulaParticipacao
    {
        [Key]
        public int Id { get; set; }

        public virtual DashboardAula Aula { get; set; }
        public virtual DashboardParticipacao Participacao { get; set; }
    }

    public class DashboardAula
    {
        public int Id { get; set; }

        public EventoTipo EventoTipoId { get; set; }
        public DateTime Data { get; set; }
        public string Descricao { get; set; }
        public string Observacao { get; set; }
        public int DuracaoMinutos { get; set; }
        public bool Finalizado { get; set; }
        public bool Active { get; set; }

        public string Sala { get; set; }
        public int Andar { get; set; }
        public int NumeroSala { get; set; }

        public string Tema { get; set; }
        public int Semana { get; set; }
        public string RoteiroCorLegenda { get; set; }

        public string Turma { get; set; }
        public string Professor { get; set; }
        public string CorLegenda { get; set; }

        public virtual Feriado Feriado { get; set; }
    }

    public class DashboardParticipacao
    {
        public int Id { get; set; }

        public bool? Presente { get; set; }
        public string Observacao { get; set; }
        public DateTime? Deactivated { get; set; }
        public bool Active { get; set; }

        public string ApostilaAbaco { get; set; }
        public string ApostilaAH { get; set; }
        public int? NumeroPaginaAbaco { get; set; }
        public int? NumeroPaginaAH { get; set; }

        public DateTime? AlunoContactado { get; set; }
        public StatusContato? StatusContatoId { get; set; }
        public string ContatoObservacao { get; set; }
    }

    public class Feriado
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}