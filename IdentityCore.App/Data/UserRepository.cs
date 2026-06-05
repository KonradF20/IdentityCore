using Microsoft.Data.SqlClient;
using IdentityCore.App.Models;

namespace IdentityCore.App.Data;

public class UserRepository
{
    public User? GetActiveUserByUsername(string username)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
                Id,
                Username,
                PasswordHash,
                FullName,
                Role,
                IsActive,
                CreatedAt
            FROM dbo.Users
            WHERE Username = @Username
              AND IsActive = 1;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", username);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}