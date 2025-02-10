using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Aula {
    public class CalendarioResponse : CalendarioList {
        public List<CalendarioAlunoList> Alunos { get; set; }
    }
}
