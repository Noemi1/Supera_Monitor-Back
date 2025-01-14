namespace Supera_Monitor_Back.Entities {
    public abstract class BaseEntity {

        public int? Account_Created_Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? Deactivated { get; set; }
        public bool Active => !Deactivated.HasValue;

        public Account? Account_Created { get; set; } = null!;
    }
}
