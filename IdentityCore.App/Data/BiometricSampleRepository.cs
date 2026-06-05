using Microsoft.Data.SqlClient;

namespace IdentityCore.App.Data;

public class BiometricSampleRepository
{
    public void SaveOrUpdateSample(
        int personId,
        string biometricType,
        string filePath,
        double qualityScore,
        string notes)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string deleteQuery = @"
            DELETE FROM dbo.BiometricSamples
            WHERE PersonId = @PersonId
              AND BiometricType = @BiometricType;
        ";

        using (var deleteCommand = new SqlCommand(deleteQuery, connection))
        {
            deleteCommand.Parameters.AddWithValue("@PersonId", personId);
            deleteCommand.Parameters.AddWithValue("@BiometricType", biometricType);
            deleteCommand.ExecuteNonQuery();
        }

        const string insertQuery = @"
            INSERT INTO dbo.BiometricSamples
            (
                PersonId,
                BiometricType,
                FilePath,
                QualityScore,
                Notes
            )
            VALUES
            (
                @PersonId,
                @BiometricType,
                @FilePath,
                @QualityScore,
                @Notes
            );
        ";

        using var insertCommand = new SqlCommand(insertQuery, connection);

        insertCommand.Parameters.AddWithValue("@PersonId", personId);
        insertCommand.Parameters.AddWithValue("@BiometricType", biometricType);
        insertCommand.Parameters.AddWithValue("@FilePath", filePath);
        insertCommand.Parameters.AddWithValue("@QualityScore", qualityScore);
        insertCommand.Parameters.AddWithValue("@Notes", notes);

        insertCommand.ExecuteNonQuery();
    }

    public void DeleteSamplesForPerson(int personId)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            DELETE FROM dbo.BiometricSamples
            WHERE PersonId = @PersonId;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PersonId", personId);

        command.ExecuteNonQuery();
    }
}