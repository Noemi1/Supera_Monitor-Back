using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Aula {
    public class CalendarioResponse : CalendarioList {
        public List<CalendarioAlunoList> Alunos { get; set; } = new List<CalendarioAlunoList>();
        public List<PerfilCognitivoModel> PerfilCognitivo { get; set; } = new List<PerfilCognitivoModel>();
    }
}
