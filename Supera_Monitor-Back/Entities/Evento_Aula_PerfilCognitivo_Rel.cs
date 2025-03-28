namespace Supera_Monitor_Back.Entities;

public partial class Evento_Aula_PerfilCognitivo_Rel {
    public int Id { get; set; }

    public int Evento_Aula_Id { get; set; }

    public int PerfilCognitivo_Id { get; set; }

    public virtual Evento_Aula Evento_Aula { get; set; } = null!;

    public virtual PerfilCognitivo PerfilCognitivo { get; set; } = null!;
}
