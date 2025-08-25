
namespace Supera_Monitor_Back.Models.Dashboard
{
	public class Dashboard_Response
	{
		public List<Dashboard_Roteiro> Roteiros { get; set; } = new List<Dashboard_Roteiro>();
		public List<Dashboard_Aluno> Alunos { get; set; } = new List<Dashboard_Aluno>();
	}
}
