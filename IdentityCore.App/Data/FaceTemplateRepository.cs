using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IdentityCore.App.Models;

namespace IdentityCore.App.Data;

public class FaceTemplateRepository
{
    public int Add(FaceTemplate template)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            INSERT INTO dbo.FaceTemplates
            (
                PersonId,
                SourceImagePath,
                SourceType,
                EmbeddingJson,
                QualityScore,
                ModelName
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @PersonId,
                @SourceImagePath,
                @SourceType,
                @EmbeddingJson,
                @QualityScore,
                @ModelName
            );
        ";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@PersonId", template.PersonId);
        command.Parameters.AddWithValue("@SourceImagePath", template.SourceImagePath);
        command.Parameters.AddWithValue("@SourceType", template.SourceType);
        command.Parameters.AddWithValue("@EmbeddingJson", template.EmbeddingJson);
        command.Parameters.AddWithValue("@QualityScore", template.QualityScore);
        command.Parameters.AddWithValue("@ModelName", template.ModelName);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<FaceTemplate> GetAll()
    {
        var templates = new List<FaceTemplate>();

        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
                Id,
                PersonId,
                SourceImagePath,
                SourceType,
                EmbeddingJson,
                QualityScore,
                ModelName,
                CreatedAt
            FROM dbo.FaceTemplates
            ORDER BY CreatedAt DESC;
        ";

        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            templates.Add(new FaceTemplate
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PersonId = reader.GetInt32(reader.GetOrdinal("PersonId")),
                SourceImagePath = reader.GetString(reader.GetOrdinal("SourceImagePath")),
                SourceType = reader.GetString(reader.GetOrdinal("SourceType")),
                EmbeddingJson = reader.GetString(reader.GetOrdinal("EmbeddingJson")),
                QualityScore = reader.GetDouble(reader.GetOrdinal("QualityScore")),
                ModelName = reader.GetString(reader.GetOrdinal("ModelName")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        return templates;
    }

    public List<FaceTemplate> GetByPersonId(int personId)
    {
        var templates = new List<FaceTemplate>();

        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
                Id,
                PersonId,
                SourceImagePath,
                SourceType,
                EmbeddingJson,
                QualityScore,
                ModelName,
                CreatedAt
            FROM dbo.FaceTemplates
            WHERE PersonId = @PersonId
            ORDER BY CreatedAt DESC;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PersonId", personId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            templates.Add(new FaceTemplate
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PersonId = reader.GetInt32(reader.GetOrdinal("PersonId")),
                SourceImagePath = reader.GetString(reader.GetOrdinal("SourceImagePath")),
                SourceType = reader.GetString(reader.GetOrdinal("SourceType")),
                EmbeddingJson = reader.GetString(reader.GetOrdinal("EmbeddingJson")),
                QualityScore = reader.GetDouble(reader.GetOrdinal("QualityScore")),
                ModelName = reader.GetString(reader.GetOrdinal("ModelName")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        return templates;
    }

    public int Count()
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.FaceTemplates;", connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int CountByPersonId(int personId)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
        SELECT COUNT(*)
        FROM dbo.FaceTemplates
        WHERE PersonId = @PersonId;
    ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PersonId", personId);

        return Convert.ToInt32(command.ExecuteScalar());
    }
}