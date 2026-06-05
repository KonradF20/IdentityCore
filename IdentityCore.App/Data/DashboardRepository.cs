using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IdentityCore.App.Models;

namespace IdentityCore.App.Data;

public class DashboardRepository
{
    public int GetTotalPersons()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.Persons;");
    }

    public int GetActivePersons()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.Persons WHERE Status = N'Aktywny';");
    }

    public int GetFaceSamples()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.BiometricSamples WHERE BiometricType = N'Twarz';");
    }

    public int GetFingerprintSamples()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.BiometricSamples WHERE BiometricType = N'Odcisk palca';");
    }

    public int GetTodayAttempts()
    {
        return ExecuteCount(@"
            SELECT COUNT(*)
            FROM dbo.IdentificationAttempts
            WHERE CAST(AttemptDate AS DATE) = CAST(SYSDATETIME() AS DATE);
        ");
    }

    public List<IdentificationAttempt> GetRecentAttempts(int limit = 10)
    {
        var attempts = new List<IdentificationAttempt>();

        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT TOP (@Limit)
                Id,
                AttemptDate,
                BiometricType,
                InputFilePath,
                BestMatchPersonId,
                BestMatchPersonName,
                SimilarityScore,
                Decision,
                OperatorUsername,
                Status
            FROM dbo.IdentificationAttempts
            ORDER BY AttemptDate DESC;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Limit", limit);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            attempts.Add(new IdentificationAttempt
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                AttemptDate = reader.GetDateTime(reader.GetOrdinal("AttemptDate")),
                BiometricType = reader.GetString(reader.GetOrdinal("BiometricType")),

                InputFilePath = reader.IsDBNull(reader.GetOrdinal("InputFilePath"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("InputFilePath")),

                BestMatchPersonId = reader.IsDBNull(reader.GetOrdinal("BestMatchPersonId"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("BestMatchPersonId")),

                BestMatchPersonName = reader.IsDBNull(reader.GetOrdinal("BestMatchPersonName"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("BestMatchPersonName")),

                SimilarityScore = reader.GetDouble(reader.GetOrdinal("SimilarityScore")),
                Decision = reader.GetString(reader.GetOrdinal("Decision")),
                OperatorUsername = reader.GetString(reader.GetOrdinal("OperatorUsername")),
                Status = reader.GetString(reader.GetOrdinal("Status"))
            });
        }

        return attempts;
    }

    private int ExecuteCount(string query)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        using var command = new SqlCommand(query, connection);
        var result = command.ExecuteScalar();

        return Convert.ToInt32(result);
    }
}