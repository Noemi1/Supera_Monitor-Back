namespace Supera_Monitor_Back.Entities.Views {
    public class AccountList : BaseList {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public DateTime? PasswordReset { get; set; }

        public int Role_Id { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
