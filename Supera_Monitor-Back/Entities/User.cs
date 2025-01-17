namespace Supera_Monitor_Back.Entities {
    public class User : _BaseEntity {
        public User()
        {
            Account = new HashSet<Account>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public ICollection<Account> Account { get; set; }
    }
}
