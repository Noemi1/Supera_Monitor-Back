namespace Supera_Monitor_Back.Entities;

public partial class Professor_AgendaPedagogica_Rel {
    public int Id { get; set; }

    public int AgendaPedagogica_Id { get; set; }

    public int Professor_Id { get; set; }

    public bool? Presente { get; set; }

    public virtual Professor_AgendaPedagogica AgendaPedagogica { get; set; } = null!;

    public virtual Professor Professor { get; set; } = null!;
}
