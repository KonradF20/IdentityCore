using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IdentityCore.App.Models;

namespace IdentityCore.App.Data;

public class FingerprintTemplateRepository
{
    public int Add(FingerprintTemplateRecord template)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            INSERT INTO dbo.FingerprintTemplates
            (
                PersonId,
                FingerPosition,
                SourceImagePath,
                SourceType,
                TemplateData,
                QualityScore,
                AlgorithmName,
                Dpi,
                CreatedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @PersonId,
                @FingerPosition,
                @SourceImagePath,
                @SourceType,
                @TemplateData,
                @QualityScore,
                @AlgorithmName,
                @Dpi,
                @CreatedAt
            );
        ";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@PersonId", template.PersonId);
        command.Parameters.AddWithValue("@FingerPosition", template.FingerPosition);
        command.Parameters.AddWithValue("@SourceImagePath", template.SourceImagePath);
        command.Parameters.AddWithValue("@SourceType", template.SourceType);
        command.Parameters.AddWithValue("@TemplateData", template.TemplateData);
        command.Parameters.AddWithValue("@QualityScore", template.QualityScore);
        command.Parameters.AddWithValue("@AlgorithmName", template.AlgorithmName);
        command.Parameters.AddWithValue("@Dpi", template.Dpi);
        command.Parameters.AddWithValue("@CreatedAt", template.CreatedAt);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<FingerprintTemplateRecord> GetAll()
    {
        var templates = new List<FingerprintTemplateRecord>();

        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
                Id,
                PersonId,
                FingerPosition,
                SourceImagePath,
                SourceType,
                TemplateData,
                QualityScore,
                AlgorithmName,
                Dpi,
                CreatedAt
            FROM dbo.FingerprintTemplates
            ORDER BY Id;
        ";

        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            templates.Add(new FingerprintTemplateRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PersonId = reader.GetInt32(reader.GetOrdinal("PersonId")),
                FingerPosition = reader.GetString(reader.GetOrdinal("FingerPosition")),
                SourceImagePath = reader.GetString(reader.GetOrdinal("SourceImagePath")),
                SourceType = reader.GetString(reader.GetOrdinal("SourceType")),
                TemplateData = (byte[])reader["TemplateData"],
                QualityScore = reader.GetDouble(reader.GetOrdinal("QualityScore")),
                AlgorithmName = reader.GetString(reader.GetOrdinal("AlgorithmName")),
                Dpi = reader.GetInt32(reader.GetOrdinal("Dpi")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        return templates;
    }

    public int Count()
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.FingerprintTemplates;", connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int CountByPersonId(int personId)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT COUNT(*)
            FROM dbo.FingerprintTemplates
            WHERE PersonId = @PersonId;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PersonId", personId);

        return Convert.ToInt32(command.ExecuteScalar());
    }
}
