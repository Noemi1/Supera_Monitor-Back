namespace Supera_Monitor_Back.Models.Aluno;

public class CreateAlunoRequest {
    public int Turma_Id { get; set; }
    public int Pessoa_Id { get; set; }
    public int? Apostila_Kit_Id { get; set; }
    public int PerfilCognitivo_Id { get; set; }

    public DateTime DataInicioVigencia { get; set; }
    public DateTime? DataFimVigencia { get; set; }
    public bool RestricaoMobilidade { get; set; }
    public DateTime? PrimeiraAula { get; set; }

    public string? Aluno_Foto { get; set; }

    public string AspNetUsers_Created_Id { get; set; } = string.Empty;
}
