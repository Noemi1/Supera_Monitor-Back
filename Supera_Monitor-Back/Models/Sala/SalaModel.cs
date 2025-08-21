namespace Supera_Monitor_Back.Models.Sala {
    public class SalaModel {
        public int Id { get; set; }
        public int NumeroSala { get; set; }
        public int Andar { get; set; }

        public string Descricao { get; set; } = string.Empty;
    }
}
