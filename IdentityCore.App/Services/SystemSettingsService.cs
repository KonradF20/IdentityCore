using System;
using System.Collections.Generic;
using System.Globalization;
using IdentityCore.App.Data;
using IdentityCore.App.Models;
using Microsoft.Data.SqlClient;

namespace IdentityCore.App.Services;

public class SystemSettingsService
{
    public const string FaceMatchThresholdKey = "Biometrics.Face.MatchThreshold";
    public const string FaceMinimumMarginKey = "Biometrics.Face.MinimumMargin";
    public const string FingerprintMatchThresholdKey = "Biometrics.Fingerprint.MatchThreshold";
    public const string FingerprintMinimumMarginKey = "Biometrics.Fingerprint.MinimumMargin";

    private static readonly Dictionary<string, double> DefaultDoubleValues = new()
    {
        [FaceMatchThresholdKey] = 60.0,
        [FaceMinimumMarginKey] = 15.0,
        [FingerprintMatchThresholdKey] = 85.0,
        [FingerprintMinimumMarginKey] = 15.0
    };

    private static readonly Dictionary<string, string> Descriptions = new()
    {
        [FaceMatchThresholdKey] = "Minimalny wynik podobieństwa twarzy wymagany do automatycznego dopasowania.",
        [FaceMinimumMarginKey] = "Minimalna przewaga najlepszego wyniku twarzy nad drugim kandydatem.",
        [FingerprintMatchThresholdKey] = "Minimalny score SourceAFIS wymagany do dopasowania odcisku palca.",
        [FingerprintMinimumMarginKey] = "Minimalna przewaga najlepszego wyniku odcisku nad drugim kandydatem."
    };

    public BiometricDecisionSettings GetBiometricDecisionSettings()
    {
        EnsureTableAndDefaults();

        return new BiometricDecisionSettings
        {
            FaceMatchThreshold = GetDouble(FaceMatchThresholdKey),
            FaceMinimumMargin = GetDouble(FaceMinimumMarginKey),
            FingerprintMatchThreshold = GetDouble(FingerprintMatchThresholdKey),
            FingerprintMinimumMargin = GetDouble(FingerprintMinimumMarginKey)
        };
    }

    public void SaveBiometricDecisionSettings(BiometricDecisionSettings settings)
    {
        EnsureTableAndDefaults();

        UpsertDouble(FaceMatchThresholdKey, Clamp(settings.FaceMatchThreshold, 0, 100));
        UpsertDouble(FaceMinimumMarginKey, Clamp(settings.FaceMinimumMargin, 0, 100));
        UpsertDouble(FingerprintMatchThresholdKey, Clamp(settings.FingerprintMatchThreshold, 0, 250));
        UpsertDouble(FingerprintMinimumMarginKey, Clamp(settings.FingerprintMinimumMargin, 0, 250));
    }

    public void ResetBiometricDecisionSettings()
    {
        EnsureTableAndDefaults();

        foreach (var item in DefaultDoubleValues)
        {
            UpsertDouble(item.Key, item.Value);
        }
    }

    private double GetDouble(string key)
    {
        var defaultValue = DefaultDoubleValues[key];

        try
        {
            using var connection = DatabaseConnection.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SettingValue FROM dbo.SystemSettings WHERE SettingKey = @key;";
            command.Parameters.AddWithValue("@key", key);

            var value = command.ExecuteScalar()?.ToString();

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            {
                return parsed;
            }
        }
        catch
        {
            // Jeżeli tabela ustawień nie istnieje albo baza jest niedostępna, moduły biometryczne
            // nadal działają na wartościach domyślnych zaszytych w aplikacji.
        }

        return defaultValue;
    }

    private void UpsertDouble(string key, double value)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
MERGE dbo.SystemSettings AS target
USING (SELECT @key AS SettingKey) AS source
ON target.SettingKey = source.SettingKey
WHEN MATCHED THEN
    UPDATE SET
        SettingValue = @value,
        Description = @description,
        UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, Description)
    VALUES (@key, @value, @description);";

        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value.ToString("0.###", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@description", Descriptions[key]);
        command.ExecuteNonQuery();
    }

    private void EnsureTableAndDefaults()
    {
        try
        {
            using var connection = DatabaseConnection.CreateConnection();
            connection.Open();

            using (var createCommand = connection.CreateCommand())
            {
                createCommand.CommandText = @"
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        SettingKey NVARCHAR(120) NOT NULL PRIMARY KEY,
        SettingValue NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
END;";
                createCommand.ExecuteNonQuery();
            }

            foreach (var item in DefaultDoubleValues)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = @key)
BEGIN
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description)
    VALUES (@key, @value, @description);
END;";
                command.Parameters.AddWithValue("@key", item.Key);
                command.Parameters.AddWithValue("@value", item.Value.ToString("0.###", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@description", Descriptions[item.Key]);
                command.ExecuteNonQuery();
            }
        }
        catch
        {
            // Brak dodatkowego komunikatu w UI. Jeżeli zapis nie jest możliwy,
            // aplikacja korzysta z wartości domyślnych.
        }
    }

    private static double Clamp(double value, double min, double max)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return min;
        }

        return Math.Clamp(value, min, max);
    }
}
