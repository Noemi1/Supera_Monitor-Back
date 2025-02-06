namespace Supera_Monitor_Back.Models.Pessoa {
    public class UpdatePessoaRequest {
        // Nomenclatura Pessoa_Id por extenso pra não dar confusão com o mapper
        public int Pessoa_Id { get; set; }

        public string? Nome { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Endereco { get; set; } = string.Empty;
        public string? Observacao { get; set; } = string.Empty;
        public string? Telefone { get; set; } = string.Empty;
        public string? Celular { get; set; } = string.Empty;
        public string? DataNascimento { get; set; } = string.Empty;

        public int? Pessoa_Sexo_Id { get; set; }
    }
}
