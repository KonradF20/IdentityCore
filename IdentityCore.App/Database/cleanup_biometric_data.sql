/*
    IdentityCore - opcjonalne czyszczenie danych biometrycznych

    KIEDY URUCHAMIAĆ:
    - tylko gdy chcesz wyczyścić zarejestrowane próbki twarzy/odcisków i historię prób,
      bez usuwania osób oraz bez odtwarzania całej bazy.

    DOMYŚLNIE:
    - czyści dane twarzy i dane odcisków.
    - jeśli chcesz wyczyścić tylko jeden moduł, zmień wartości zmiennych poniżej.
*/

USE IdentityCoreDb;
GO

DECLARE @CleanFace BIT = 1;
DECLARE @CleanFingerprint BIT = 1;

IF @CleanFace = 1
BEGIN
    DELETE FROM dbo.MatchResults
    WHERE IdentificationAttemptId IN
    (
        SELECT Id
        FROM dbo.IdentificationAttempts
        WHERE BiometricType = N'Twarz'
    );

    DELETE FROM dbo.IdentificationAttempts
    WHERE BiometricType = N'Twarz';

    IF OBJECT_ID(N'dbo.FaceTemplates', N'U') IS NOT NULL
    BEGIN
        DELETE FROM dbo.FaceTemplates;
        DBCC CHECKIDENT ('dbo.FaceTemplates', RESEED, 0);
    END

    DELETE FROM dbo.BiometricSamples
    WHERE BiometricType = N'Twarz';

    UPDATE dbo.Persons
    SET FaceImagePath = NULL;
END

IF @CleanFingerprint = 1
BEGIN
    DELETE FROM dbo.MatchResults
    WHERE IdentificationAttemptId IN
    (
        SELECT Id
        FROM dbo.IdentificationAttempts
        WHERE BiometricType = N'Odcisk palca'
    );

    DELETE FROM dbo.IdentificationAttempts
    WHERE BiometricType = N'Odcisk palca';

    IF OBJECT_ID(N'dbo.FingerprintTemplates', N'U') IS NOT NULL
    BEGIN
        DELETE FROM dbo.FingerprintTemplates;
        DBCC CHECKIDENT ('dbo.FingerprintTemplates', RESEED, 0);
    END

    DELETE FROM dbo.BiometricSamples
    WHERE BiometricType = N'Odcisk palca';

    UPDATE dbo.Persons
    SET FingerprintImagePath = NULL;
END
GO

SELECT COUNT(*) AS FaceTemplatesCount
FROM dbo.FaceTemplates;

SELECT COUNT(*) AS FingerprintTemplatesCount
FROM dbo.FingerprintTemplates;

SELECT BiometricType, COUNT(*) AS AttemptsCount
FROM dbo.IdentificationAttempts
GROUP BY BiometricType;

SELECT Id, PersonCode, FirstName, LastName, FaceImagePath, FingerprintImagePath
FROM dbo.Persons
ORDER BY Id;
GO
