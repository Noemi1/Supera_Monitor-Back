namespace Supera_Monitor_Back.Models.Eventos.Participacao;

public class CancelarParticipacaoRequest {
    public int Participacao_Id { get; set; }
    public string? Observacao { get; set; }
    public string? ContatoObservacao { get; set; }
    public DateTime? AlunoContactado { get; set; }
    public int? StatusContato_Id { get; set; }
    public int? ReposicaoDe_Evento_Id { get; set; }
}
