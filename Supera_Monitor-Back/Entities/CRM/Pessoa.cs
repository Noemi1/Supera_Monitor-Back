namespace Supera_Monitor_Back.Entities.CRM {
    public partial class Pessoa {
        public int Id { get; set; }

        public string? Nome { get; set; }

        public string? Email { get; set; }

        public string? Endereco { get; set; }

        public string? Observacao { get; set; }

        public string? Telefone { get; set; }

        public string? Celular { get; set; }

        public DateTime DataEntrada { get; set; }

        public int? Pessoa_FaixaEtaria_Id { get; set; }

        public int? Pessoa_Origem_Id { get; set; }

        public int? Pessoa_Status_Id { get; set; }

        public string? RG { get; set; }

        public string? CPF { get; set; }

        public string? aspnetusers_Id { get; set; }

        public int? Pessoa_Sexo_Id { get; set; }

        public DateTime? DataNascimento { get; set; }

        public DateTime DataCadastro { get; set; }

        public int Unidade_Id { get; set; }

        public int? Pessoa_Origem_Canal_Id { get; set; }

        public int? Pessoa_Indicou_Id { get; set; }

        public int? LandPage_Id { get; set; }

        public int? Pessoa_Geracao_Id { get; set; }
    }

    public enum PessoaStatus {
        Matriculado = 5
    }
}
