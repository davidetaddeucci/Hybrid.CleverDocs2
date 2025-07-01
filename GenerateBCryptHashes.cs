using System;

namespace HashGenerator
{
    class Program
    {
        static void Main()
        {
            // Generate real BCrypt hashes for the test passwords
            string password1 = "Florealia2025!";
            string password2 = "Maremmabona1!";

            // Using BCrypt.Net-Next library (same as WebServices)
            string hash1 = BCrypt.Net.BCrypt.HashPassword(password1, BCrypt.Net.BCrypt.GenerateSalt(12));
            string hash2 = BCrypt.Net.BCrypt.HashPassword(password2, BCrypt.Net.BCrypt.GenerateSalt(12));

            Console.WriteLine($"Hash for {password1}:");
            Console.WriteLine($"'{hash1}'");
            Console.WriteLine();
            Console.WriteLine($"Hash for {password2}:");
            Console.WriteLine($"'{hash2}'");
            Console.WriteLine();

            // Verify the hashes work
            Console.WriteLine("Verification tests:");
            Console.WriteLine($"Password1 verification: {BCrypt.Net.BCrypt.Verify(password1, hash1)}");
            Console.WriteLine($"Password2 verification: {BCrypt.Net.BCrypt.Verify(password2, hash2)}");

            // Generate SQL update statements
            Console.WriteLine();
            Console.WriteLine("SQL Update statements:");
            Console.WriteLine($"UPDATE \"Users\" SET \"PasswordHash\" = '{hash1}' WHERE \"Email\" = 'info@hybrid.it';");
            Console.WriteLine($"UPDATE \"Users\" SET \"PasswordHash\" = '{hash2}' WHERE \"Email\" = 'info@microsis.it';");
            Console.WriteLine($"UPDATE \"Users\" SET \"PasswordHash\" = '{hash2}' WHERE \"Email\" = 'r.antoniucci@microsis.it';");
            Console.WriteLine($"UPDATE \"Users\" SET \"PasswordHash\" = '{hash2}' WHERE \"Email\" = 'm.bevilacqua@microsis.it';");
        }
    }
}
