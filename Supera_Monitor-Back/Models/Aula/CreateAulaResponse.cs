using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models.Aluno;

namespace Supera_Monitor_Back.Models.Aula {
    public class CreateAulaResponse : CalendarioList {
        public List<AlunoListWithChecklist> Alunos { get; set; } = new List<AlunoListWithChecklist>();
    }
}
