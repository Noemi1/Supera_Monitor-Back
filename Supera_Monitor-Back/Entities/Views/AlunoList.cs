namespace Supera_Monitor_Back.Entities.Views {
    public partial class AlunoList {
        public int Id { get; set; }

        public int Pessoa_Id { get; set; }

        public string? Nome { get; set; }

        public DateTime DataCadastro { get; set; }

        public DateTime DataEntrada { get; set; }

        public DateTime? DataNascimento { get; set; }

        public string? CPF { get; set; }

        public string? RG { get; set; }

        public string? Celular { get; set; }

        public string? Telefone { get; set; }

        public string? Email { get; set; }

        public string? Observacao { get; set; }

        public string? Endereco { get; set; }

        public int Unidade_Id { get; set; }

        public int Turma_Id { get; set; }

        public string? Turma { get; set; }

        public int? Professor_Id { get; set; }

        public string? Professor { get; set; }

        public int? Pessoa_FaixaEtaria_Id { get; set; }

        public string? Pessoa_FaixaEtaria { get; set; }

        public int? Pessoa_Geracao_Id { get; set; }

        public string? Pessoa_Geracao { get; set; }

        public int? Pessoa_Indicou_Id { get; set; }

        public string? Pessoa_Indicou { get; set; }

        public int? Pessoa_Origem_Canal_Id { get; set; }

        public string? Pessoa_Origem_Canal { get; set; }

        public int? Pessoa_Origem_Id { get; set; }

        public string? Pessoa_Origem { get; set; }

        public int? Pessoa_Sexo_Id { get; set; }

        public string? Pessoa_Sexo { get; set; }

        public int? Pessoa_Status_Id { get; set; }

        public string? Pessoa_Status { get; set; }
    }
}
