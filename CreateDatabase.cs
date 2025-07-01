using Npgsql;
using System;

class Program
{
    static void Main()
    {
        var connectionString = "Host=192.168.1.4;Port=5433;Database=postgres;Username=admin;Password=Florealia2025!;";
        
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            
            using var command = new NpgsqlCommand("CREATE DATABASE cleverdocs OWNER admin;", connection);
            command.ExecuteNonQuery();
            
            Console.WriteLine("✅ Database 'cleverdocs' created successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating database: {ex.Message}");
        }
    }
}
