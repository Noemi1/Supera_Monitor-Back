namespace Supera_Monitor_Back.Entities.Views {
    public class TurmaList {
        public int Id { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan Horario { get; set; }

        public int? Professor_Id { get; set; }

        public int Turma_Tipo_Id { get; set; }
        public string Turma_Tipo { get; set; } = string.Empty;
    }
}

//namespace Supera_Monitor_Back.Entities {
//    public class TurmaList {
//        public int Id { get; set; }
//        public int DiaSemana { get; set; }
//        public TimeSpan Horario { get; set; }

//        public int? Professor_Id { get; set; }

//        public int? Turma_Tipo_Id { get; set; }
//        public string Turma_Tipo { get; set; } = string.Empty;
//    }
//}


//namespace Supera_Monitor_Back.Entities.Views {
//    public class AccountList : BaseList {
//        public int Id { get; set; }
//        public string Name { get; set; } = string.Empty;
//        public string Email { get; set; } = string.Empty;
//        public string Phone { get; set; } = string.Empty;
//        public DateTime? Verified { get; set; }
//        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
//        public DateTime? PasswordReset { get; set; }

//        public int Role_Id { get; set; }
//        public string Role { get; set; } = string.Empty;
//    }
//}
