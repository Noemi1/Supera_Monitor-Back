using System.Security.Cryptography;
using BC = BCrypt.Net.BCrypt;

namespace Supera_Monitor_Back.Helpers {
    public static class Utils {

        // Generate a random sequence of bytes and convert them to hex string
        public static string RandomTokenString() {
            using RandomNumberGenerator rngCryptoServiceProvider = RandomNumberGenerator.Create();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        // Generates pseudo-random password for password resets and such
        public static (string randomPassword, string passwordHash) GenerateRandomHashedPassword() {
            Random generator = new();
            string randomPassword = generator.Next(0, 1000000).ToString("D6");
            return (randomPassword, BC.HashPassword(randomPassword));
        }


        // um dia isso vai dar bigode
        // but not today c:
        public static string GenerateRM(DataContext db) {
            Random RNG = new();
            string randomRM;

            do {
                randomRM = RNG.Next(0, 100000).ToString("D5");
            }
            while (db.Alunos.Any(x => x.RM == randomRM));

            return randomRM;
        }
    }
}
