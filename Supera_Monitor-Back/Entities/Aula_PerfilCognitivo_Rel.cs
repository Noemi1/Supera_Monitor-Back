namespace Supera_Monitor_Back.Entities;

public partial class Aula_PerfilCognitivo_Rel {
    public int Id { get; set; }

    public int Aula_Id { get; set; }

    public int PerfilCognitivo_Id { get; set; }

    public virtual PerfilCognitivo PerfilCognitivo { get; set; } = null!;
}
