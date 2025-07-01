using System;

class Program
{
    static void Main()
    {
        // Simulate BCrypt hashing (simplified for demo)
        string password1 = "Florealia2025!";
        string password2 = "Maremmabona1!";
        
        // For demo purposes, using simple hash (in real app would use BCrypt.Net)
        string hash1 = "$2a$11$" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password1)).Replace("=", "").Substring(0, 53);
        string hash2 = "$2a$11$" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password2)).Replace("=", "").Substring(0, 53);
        
        Console.WriteLine($"Hash for {password1}: {hash1}");
        Console.WriteLine($"Hash for {password2}: {hash2}");
    }
}
