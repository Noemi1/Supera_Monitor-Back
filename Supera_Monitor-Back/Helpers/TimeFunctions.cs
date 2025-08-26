namespace Supera_Monitor_Back.Helpers;


public class TimeFunctions {
    public static DateTime HoraAtualBR() {
        string timeZoneId = "E. South America Standard Time";

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        DateTime currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);

        return currentTime;
    }
}


