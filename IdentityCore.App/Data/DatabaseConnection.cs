using Microsoft.Data.SqlClient;

namespace IdentityCore.App.Data;

public static class DatabaseConnection
{
    private const string ConnectionString =
        @"Server=localhost;Database=IdentityCoreDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public static SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}