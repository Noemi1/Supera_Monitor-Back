namespace Supera_Monitor_Back.Models.Eventos
{
    public class FeriadoResponse
    {
        public string type { get; set; } = null!; 
        public string name { get; set; } = null!;
        public string level { get; set; } = null!;
        public DateTime date { get; set; }
	}
}
