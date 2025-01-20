namespace Supera_Monitor_Back.Models.Accounts {
    public class UpdateAccountRequest {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int Role_Id { get; set; }
    }
}
