/*
    IdentityCore - aktualizacja istniejącej starszej bazy

    KIEDY URUCHAMIAĆ:
    - tylko wtedy, gdy masz starą bazę IdentityCoreDb i nie chcesz jej odtwarzać od zera,
    - skrypt jest idempotentny: dodaje brakujące tabele/kolumny/indeksy/ustawienia,
      ale nie usuwa danych.

    PRZY ŚWIEŻEJ INSTALACJI:
    - nie uruchamiaj tego skryptu,
    - wystarczy Database/create_database.sql.
*/

USE IdentityCoreDb;
GO

IF COL_LENGTH('dbo.Persons', 'ProfileImagePath') IS NULL
BEGIN
    ALTER TABLE dbo.Persons
    ADD ProfileImagePath NVARCHAR(500) NULL;
END
GO

IF OBJECT_ID(N'dbo.FaceTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FaceTemplates
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PersonId INT NOT NULL,
        SourceImagePath NVARCHAR(500) NOT NULL,
        SourceType NVARCHAR(40) NOT NULL DEFAULT N'Plik',
        EmbeddingJson NVARCHAR(MAX) NOT NULL,
        QualityScore FLOAT NOT NULL DEFAULT 0,
        ModelName NVARCHAR(120) NOT NULL DEFAULT N'arcface.onnx',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_FaceTemplates_Persons
            FOREIGN KEY (PersonId) REFERENCES dbo.Persons(Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FaceTemplates_PersonId'
      AND object_id = OBJECT_ID(N'dbo.FaceTemplates')
)
BEGIN
    CREATE INDEX IX_FaceTemplates_PersonId
    ON dbo.FaceTemplates(PersonId);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FaceTemplates_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.FaceTemplates')
)
BEGIN
    CREATE INDEX IX_FaceTemplates_CreatedAt
    ON dbo.FaceTemplates(CreatedAt);
END
GO

IF OBJECT_ID(N'dbo.FingerprintTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FingerprintTemplates
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PersonId INT NOT NULL,
        FingerPosition NVARCHAR(80) NOT NULL DEFAULT N'Prawy palec wskazujący',
        SourceImagePath NVARCHAR(500) NOT NULL,
        SourceType NVARCHAR(40) NOT NULL DEFAULT N'Plik',
        TemplateData VARBINARY(MAX) NOT NULL,
        QualityScore FLOAT NOT NULL DEFAULT 0,
        AlgorithmName NVARCHAR(120) NOT NULL DEFAULT N'SourceAFIS',
        Dpi INT NOT NULL DEFAULT 500,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_FingerprintTemplates_Persons
            FOREIGN KEY (PersonId) REFERENCES dbo.Persons(Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FingerprintTemplates_PersonId'
      AND object_id = OBJECT_ID(N'dbo.FingerprintTemplates')
)
BEGIN
    CREATE INDEX IX_FingerprintTemplates_PersonId
    ON dbo.FingerprintTemplates(PersonId);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FingerprintTemplates_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.FingerprintTemplates')
)
BEGIN
    CREATE INDEX IX_FingerprintTemplates_CreatedAt
    ON dbo.FingerprintTemplates(CreatedAt);
END
GO

IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        SettingKey NVARCHAR(120) NOT NULL PRIMARY KEY,
        SettingValue NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
END
GO

MERGE dbo.SystemSettings AS target
USING (VALUES
    (N'Biometrics.Face.MatchThreshold', N'60', N'Minimalny wynik podobieństwa twarzy wymagany do automatycznego dopasowania.'),
    (N'Biometrics.Face.MinimumMargin', N'15', N'Minimalna przewaga najlepszego wyniku twarzy nad drugim kandydatem.'),
    (N'Biometrics.Fingerprint.MatchThreshold', N'85', N'Minimalny score SourceAFIS wymagany do dopasowania odcisku palca.'),
    (N'Biometrics.Fingerprint.MinimumMargin', N'15', N'Minimalna przewaga najlepszego wyniku odcisku nad drugim kandydatem.')
) AS source (SettingKey, SettingValue, Description)
ON target.SettingKey = source.SettingKey
WHEN MATCHED THEN
    UPDATE SET
        Description = source.Description,
        UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, Description)
    VALUES (source.SettingKey, source.SettingValue, source.Description);
GO

SELECT N'Istniejąca baza IdentityCoreDb została zaktualizowana do aktualnej struktury.' AS Status;
GO
