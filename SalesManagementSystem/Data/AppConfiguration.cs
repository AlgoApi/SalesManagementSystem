using System.IO;
using System.Text.Json;

namespace SalesManagementSystem.Data;

public static class AppConfiguration
{
    private const string EnvironmentVariable = "SALES_DB_CONNECTION";

    public static string GetConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var candidateFiles = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Environment.CurrentDirectory, "appsettings.json")
        };

        foreach (var file in candidateFiles)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var section)
                && section.TryGetProperty("DefaultConnection", out var value))
            {
                var connectionString = value.GetString();
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    return connectionString;
                }
            }
        }

        return "Server=localhost;Database=SalesManagementDb;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
