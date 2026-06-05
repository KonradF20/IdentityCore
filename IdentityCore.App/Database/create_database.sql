/*
    IdentityCore - pełne utworzenie bazy od zera

    KIEDY URUCHAMIAĆ:
    - tylko przy świeżej instalacji albo gdy świadomie chcesz odtworzyć bazę od zera.

    UWAGA:
    - skrypt usuwa istniejące tabele aplikacji i tworzy je ponownie,
    - usuwa dane osób, prób identyfikacji, szablonów twarzy i odcisków,
    - zawiera już strukturę po wszystkich dotychczasowych poprawkach:
      ProfileImagePath, FaceTemplates, FingerprintTemplates, SystemSettings,
    - zawiera podstawowe dane startowe i domyślne progi decyzyjne.
*/

IF DB_ID(N'IdentityCoreDb') IS NULL
BEGIN
    CREATE DATABASE IdentityCoreDb;
END
GO

USE IdentityCoreDb;
GO

IF OBJECT_ID(N'dbo.MatchResults', N'U') IS NOT NULL DROP TABLE dbo.MatchResults;
IF OBJECT_ID(N'dbo.IdentificationAttempts', N'U') IS NOT NULL DROP TABLE dbo.IdentificationAttempts;
IF OBJECT_ID(N'dbo.FaceTemplates', N'U') IS NOT NULL DROP TABLE dbo.FaceTemplates;
IF OBJECT_ID(N'dbo.FingerprintTemplates', N'U') IS NOT NULL DROP TABLE dbo.FingerprintTemplates;
IF OBJECT_ID(N'dbo.BiometricSamples', N'U') IS NOT NULL DROP TABLE dbo.BiometricSamples;
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL DROP TABLE dbo.SystemSettings;
IF OBJECT_ID(N'dbo.Persons', N'U') IS NOT NULL DROP TABLE dbo.Persons;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(120) NOT NULL,
    Role NVARCHAR(40) NOT NULL DEFAULT N'Operator',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE dbo.SystemSettings
(
    SettingKey NVARCHAR(120) NOT NULL PRIMARY KEY,
    SettingValue NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE dbo.Persons
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PersonCode NVARCHAR(30) NOT NULL UNIQUE,
    FirstName NVARCHAR(80) NOT NULL,
    LastName NVARCHAR(80) NOT NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(30) NULL,
    Department NVARCHAR(120) NULL,
    Status NVARCHAR(40) NOT NULL DEFAULT N'Aktywny',
    Description NVARCHAR(1000) NULL,
    ProfileImagePath NVARCHAR(500) NULL,
    FaceImagePath NVARCHAR(500) NULL,
    FingerprintImagePath NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

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
GO

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
GO

CREATE TABLE dbo.BiometricSamples
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PersonId INT NOT NULL,
    BiometricType NVARCHAR(40) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    QualityScore FLOAT NOT NULL DEFAULT 0,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_BiometricSamples_Persons
        FOREIGN KEY (PersonId) REFERENCES dbo.Persons(Id)
        ON DELETE CASCADE
);
GO

CREATE TABLE dbo.IdentificationAttempts
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AttemptDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    BiometricType NVARCHAR(40) NOT NULL,
    InputFilePath NVARCHAR(500) NULL,
    BestMatchPersonId INT NULL,
    BestMatchPersonName NVARCHAR(200) NULL,
    SimilarityScore FLOAT NOT NULL DEFAULT 0,
    Decision NVARCHAR(100) NOT NULL,
    OperatorUsername NVARCHAR(50) NOT NULL DEFAULT N'admin',
    Status NVARCHAR(60) NOT NULL,

    CONSTRAINT FK_IdentificationAttempts_BestMatchPerson
        FOREIGN KEY (BestMatchPersonId) REFERENCES dbo.Persons(Id)
        ON DELETE SET NULL
);
GO

CREATE TABLE dbo.MatchResults
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdentificationAttemptId INT NOT NULL,
    PersonId INT NULL,
    PersonFullName NVARCHAR(200) NULL,
    PersonCode NVARCHAR(30) NULL,
    SimilarityScore FLOAT NOT NULL DEFAULT 0,
    Decision NVARCHAR(100) NOT NULL,
    Rank INT NOT NULL,

    CONSTRAINT FK_MatchResults_IdentificationAttempts
        FOREIGN KEY (IdentificationAttemptId) REFERENCES dbo.IdentificationAttempts(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_MatchResults_Persons
        FOREIGN KEY (PersonId) REFERENCES dbo.Persons(Id)
        ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.AuditLogs
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    OperatorUsername NVARCHAR(50) NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NULL,
    EntityId INT NULL,
    Description NVARCHAR(1000) NULL
);
GO

CREATE INDEX IX_Persons_PersonCode ON dbo.Persons(PersonCode);
CREATE INDEX IX_Persons_LastName ON dbo.Persons(LastName);
CREATE INDEX IX_BiometricSamples_PersonId ON dbo.BiometricSamples(PersonId);
CREATE INDEX IX_IdentificationAttempts_AttemptDate ON dbo.IdentificationAttempts(AttemptDate);
CREATE INDEX IX_MatchResults_AttemptId ON dbo.MatchResults(IdentificationAttemptId);
CREATE INDEX IX_FaceTemplates_PersonId ON dbo.FaceTemplates(PersonId);
CREATE INDEX IX_FaceTemplates_CreatedAt ON dbo.FaceTemplates(CreatedAt);
CREATE INDEX IX_FingerprintTemplates_PersonId ON dbo.FingerprintTemplates(PersonId);
CREATE INDEX IX_FingerprintTemplates_CreatedAt ON dbo.FingerprintTemplates(CreatedAt);
GO

/* Dane startowe */

INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role, IsActive)
VALUES
(N'admin', N'admin', N'Administrator Systemu', N'Administrator', 1);
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
        SettingValue = source.SettingValue,
        Description = source.Description,
        UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, Description)
    VALUES (source.SettingKey, source.SettingValue, source.Description);
GO

-- Osoby startowe nie mają gotowych próbek biometrycznych.
-- Próbki twarzy i odcisków są rejestrowane przez aplikację.
INSERT INTO dbo.Persons
(
    PersonCode,
    FirstName,
    LastName,
    DateOfBirth,
    Gender,
    Department,
    Status,
    Description,
    ProfileImagePath,
    FaceImagePath,
    FingerprintImagePath
)
VALUES
(
    N'P-000124',
    N'Jan',
    N'Kowalski',
    '1988-04-12',
    N'Mężczyzna',
    N'Administracja',
    N'Aktywny',
    N'Pracownik administracyjny zarejestrowany w systemie identyfikacji.',
    NULL,
    NULL,
    NULL
),
(
    N'P-000089',
    N'Anna',
    N'Nowak',
    '1994-09-03',
    N'Kobieta',
    N'Laboratorium',
    N'Aktywny',
    N'Osoba testowa do walidacji działania modułu identyfikacji twarzy.',
    NULL,
    NULL,
    NULL
),
(
    N'P-000012',
    N'Marek',
    N'Borkowski',
    '1981-01-24',
    N'Mężczyzna',
    N'Ochrona',
    N'Zawieszony',
    N'Konto oznaczone jako nieaktywne testowo.',
    NULL,
    NULL,
    NULL
);
GO

INSERT INTO dbo.AuditLogs
(
    OperatorUsername,
    ActionType,
    EntityName,
    EntityId,
    Description
)
VALUES
(N'admin', N'SEED_DATABASE', N'System', NULL, N'Dodano konto techniczne i osoby startowe bez gotowych próbek biometrycznych.'),
(N'admin', N'CREATE_PERSON', N'Persons', 1, N'Dodano osobę testową Jan Kowalski.'),
(N'admin', N'CREATE_PERSON', N'Persons', 2, N'Dodano osobę testową Anna Nowak.'),
(N'admin', N'CREATE_PERSON', N'Persons', 3, N'Dodano osobę testową Marek Borkowski.');
GO

SELECT N'IdentityCoreDb została utworzona i uzupełniona danymi startowymi.' AS Status;
GO
