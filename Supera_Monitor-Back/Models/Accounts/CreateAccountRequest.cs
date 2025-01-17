namespace Supera_Monitor_Back.Models.Accounts {
    public class CreateAccountRequest {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int User_Id { get; set; }
        public int Role_Id { get; set; }
    }
}
