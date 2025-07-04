﻿namespace Supera_Monitor_Back.Entities.Views;

public partial class CalendarioAlunoList {
    public int Id { get; set; }

    public int Aluno_Id { get; set; }

    public int Evento_Id { get; set; }

    public string? Checklist { get; set; }

    public int? Checklist_Id { get; set; }

    public string? Aluno { get; set; }

    public DateTime? DataNascimento { get; set; }

    public string? Celular { get; set; }

    public string? Aluno_Foto { get; set; }

    public int Turma_Id { get; set; }

    public DateTime? DataInicioVigencia { get; set; }

    public DateTime? DataFimVigencia { get; set; }

    public int? PrimeiraAula_Id { get; set; }

    public DateTime? PrimeiraAula { get; set; }

    public int? AulaZero_Id { get; set; }

    public DateTime? AulaZero { get; set; }

    public bool? RestricaoMobilidade { get; set; }

    public string? Turma { get; set; }

    public int? ReposicaoDe_Evento_Id { get; set; }

    public int? ReposicaoPara_Evento_Id { get; set; }

    public bool? Presente { get; set; }

    public int? Apostila_Kit_Id { get; set; }

    public string? Kit { get; set; }

    public string? Apostila_Abaco { get; set; }

    public string? Apostila_AH { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public string? Observacao { get; set; }

    public DateTime? Deactivated { get; set; }

    public bool Active => !Deactivated.HasValue;

    public int? PerfilCognitivo_Id { get; set; }

    public string PerfilCognitivo { get; set; } = null!;
}