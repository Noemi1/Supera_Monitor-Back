#nullable disable

namespace Supera_Monitor_Back.Entities {
    public partial class AccountRole {
        public AccountRole()
        {
            Account = new HashSet<Account>();
        }

        public int Id { get; set; }
        public string Role { get; set; }

        public virtual ICollection<Account> Account { get; set; }
    }

    public enum Role {
        Assistant = 1,
        Teacher = 2,
        Admin = 3,
    }
}
