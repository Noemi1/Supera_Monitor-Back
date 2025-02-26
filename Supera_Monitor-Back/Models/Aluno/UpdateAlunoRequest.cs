namespace Supera_Monitor_Back.Models.Aluno {
    public class UpdateAlunoRequest {
        public int Id { get; set; }

        // Alterar dados da entidade Aluno
        public int Turma_Id { get; set; }
        public int PerfilCognitivo_Id { get; set; }

        public string? Aluno_Foto { get; set; }
        public int? Apostila_Kit_Id { get; set; }

        // Alterar dados da entidade Pessoa
        public string Nome { get; set; } = string.Empty;
        public string DataNascimento { get; set; } = string.Empty;

        public string? Email { get; set; } = string.Empty;
        public string? Endereco { get; set; } = string.Empty;
        public string? Observacao { get; set; } = string.Empty;
        public string? Telefone { get; set; } = string.Empty;
        public string? Celular { get; set; } = string.Empty;
        public int? Pessoa_Sexo_Id { get; set; }
    }
}
