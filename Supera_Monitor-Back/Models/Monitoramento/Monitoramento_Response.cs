
namespace Supera_Monitor_Back.Models.Monitoramento
{
    public class Monitoramento_Response
    {
        public List<Monitoramento_Aluno> Alunos { get; set; } = new List<Monitoramento_Aluno>();

        public List<Monitoramento_Mes> MesesRoteiro { get; set; } = new List<Monitoramento_Mes>();
    }

}