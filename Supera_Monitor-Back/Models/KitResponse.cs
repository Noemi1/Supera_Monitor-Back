using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models {
    public class KitResponse {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public List<ApostilaList> Apostilas { get; set; } = new List<ApostilaList>();
    }
}


//public class CalendarioResponse : CalendarioList {
//    public List<CalendarioAlunoList> Alunos { get; set; } = new List<CalendarioAlunoList>();
//}