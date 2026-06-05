using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IdentityCore.App.Models;

namespace IdentityCore.App.Data;

public class IdentificationRepository
{
    public List<IdentificationAttempt> GetAllAttempts()
    {
        var attempts = new List<IdentificationAttempt>();

        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
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

    public int AddAttempt(IdentificationAttempt attempt)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
        INSERT INTO dbo.IdentificationAttempts
        (
            AttemptDate,
            BiometricType,
            InputFilePath,
            BestMatchPersonId,
            BestMatchPersonName,
            SimilarityScore,
            Decision,
            OperatorUsername,
            Status
        )
        OUTPUT INSERTED.Id
        VALUES
        (
            @AttemptDate,
            @BiometricType,
            @InputFilePath,
            @BestMatchPersonId,
            @BestMatchPersonName,
            @SimilarityScore,
            @Decision,
            @OperatorUsername,
            @Status
        );
    ";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@AttemptDate", attempt.AttemptDate);
        command.Parameters.AddWithValue("@BiometricType", attempt.BiometricType);
        command.Parameters.AddWithValue("@InputFilePath", string.IsNullOrWhiteSpace(attempt.InputFilePath) ? DBNull.Value : attempt.InputFilePath);
        command.Parameters.AddWithValue("@BestMatchPersonId", attempt.BestMatchPersonId.HasValue ? attempt.BestMatchPersonId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@BestMatchPersonName", string.IsNullOrWhiteSpace(attempt.BestMatchPersonName) ? DBNull.Value : attempt.BestMatchPersonName);
        command.Parameters.AddWithValue("@SimilarityScore", attempt.SimilarityScore);
        command.Parameters.AddWithValue("@Decision", attempt.Decision);
        command.Parameters.AddWithValue("@OperatorUsername", attempt.OperatorUsername);
        command.Parameters.AddWithValue("@Status", attempt.Status);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int GetTotalAttempts()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.IdentificationAttempts;");
    }

    public int GetAcceptedAttempts()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.IdentificationAttempts WHERE Status IN (N'Znaleziono', N'Zatwierdzony');");
    }

    public int GetRejectedAttempts()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.IdentificationAttempts WHERE Status IN (N'Nie znaleziono', N'Odrzucony');");
    }

    public int GetVerificationAttempts()
    {
        return ExecuteCount("SELECT COUNT(*) FROM dbo.IdentificationAttempts WHERE Status = N'Do weryfikacji';");
    }

    private int ExecuteCount(string query)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        using var command = new SqlCommand(query, connection);
        var result = command.ExecuteScalar();

        return result == null ? 0 : (int)result;
    }
}