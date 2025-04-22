using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Eventos;

public class Dashboard
{
	public bool Show { get; set; } = false;
	public bool PrimeiraAula { get; set; } = false;
	public int Roteiro_Id { get; set; }
	public int Aluno_Id { get; set; }
	public CalendarioEventoList Aula { get; set; } = null!;
	public CalendarioAlunoList Participacao { get; set; } = null!;
}