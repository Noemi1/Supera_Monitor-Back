using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Models;

namespace Supera_Monitor_Back.Helpers
{
    public class EventoUtils
    {
        public static ResponseModel ValidateEvent(Evento? evento)
        {
            if (evento is null)
                return new ResponseModel { Message = "Evento não encontrado." };

            if (evento.Deactivated.HasValue)
                return new ResponseModel { Message = $"Este evento foi cancelado às {evento.Deactivated.Value:g}" };

            if (evento.Finalizado)
                return new ResponseModel { Message = "Evento já está finalizado" };

            return new ResponseModel { Success = true, Message = "Evento válido" };
        }

        public static string GetTipo(int evento_Tipo_Id)
        {
            string tipo = "Aula";
            switch ((EventoTipo)evento_Tipo_Id)
            {
                case EventoTipo.Oficina:
                    tipo = "Oficina";
                    break;
                case EventoTipo.Aula:
                    tipo = "Aula";
                    break;
                case EventoTipo.AulaZero:
                    tipo = "Aula Zero";
                    break;
                case EventoTipo.TurmaExtra:
                    tipo = "Turma Extra";
                    break;
                case EventoTipo.Superacao:
                    tipo = "Superação";
                    break;
                case EventoTipo.Reuniao:
                    tipo = "Reunião";
                    break;
                default:
                    tipo = "Aula";
                    break;
            }
           
            return tipo;
        }
    }
}
