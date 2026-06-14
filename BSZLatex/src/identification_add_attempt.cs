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