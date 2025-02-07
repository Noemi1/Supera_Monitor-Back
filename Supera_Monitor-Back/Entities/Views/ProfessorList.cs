namespace Supera_Monitor_Back.Entities.Views {
    public partial class ProfessorList : BaseList {
        public int Id { get; set; }

        public string Nome { get; set; } = null!;

        public string Telefone { get; set; } = null!;

        public string Email { get; set; } = null!;

        public DateTime DataInicio { get; set; }

        public string CorLegenda { get; set; } = string.Empty;

        public int Account_Id { get; set; }

        public int Role_Id { get; set; }

        public string? Role { get; set; }

        public int? Professor_NivelAbaco_Id { get; set; }

        public string? NivelAbaco { get; set; } = null!;

        public int? Professor_NivelAH_Id { get; set; }

        public string? NivelAH { get; set; } = null!;
    }
}
