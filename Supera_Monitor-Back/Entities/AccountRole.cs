namespace Supera_Monitor_Back.Entities;

public partial class AccountRole {
    public int Id { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

}

public enum Role {
    Assistant = 1,
    Teacher = 2,
    Admin = 3,
}
