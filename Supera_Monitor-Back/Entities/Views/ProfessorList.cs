namespace Supera_Monitor_Back.Entities.Views {
    public class ProfessorList {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int Account_Id { get; set; }
    }
}
