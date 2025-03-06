using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Aluno {
    public class AlunoListWithChecklist : AlunoList {
        public List<AlunoChecklistView> AlunoChecklist { get; set; } = new List<AlunoChecklistView>();
    }
}
