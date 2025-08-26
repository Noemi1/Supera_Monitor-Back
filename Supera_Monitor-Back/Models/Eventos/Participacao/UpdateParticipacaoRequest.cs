namespace Supera_Monitor_Back.Models.Eventos.Participacao;

public class UpdateParticipacaoRequest {
    public int Participacao_Id { get; set; }
    public bool? Presente { get; set; }
    public string? Observacao { get; set; }
    public int? Apostila_AH_Id { get; set; }
    public int? Apostila_Abaco_Id { get; set; }
    public int? NumeroPaginaAH { get; set; }
    public int? NumeroPaginaAbaco { get; set; }
    public int? ReposicaoDe_Evento_Id { get; set; }
    public DateTime? Deactivated { get; set; }
    public string? ContatoObservacao { get; set; }
    public DateTime? AlunoContactado { get; set; }
    public int? StatusContato_Id { get; set; }
}
