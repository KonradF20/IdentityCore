public FingerprintTemplateExtractionResult CreateTemplate(string imagePath, int dpi = DefaultDpi)
{
    try
    {
        var resolvedPath = ResolveImagePath(imagePath);

        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            return new FingerprintTemplateExtractionResult
            {
                Success = false,
                Message = "Nie znaleziono obrazu odcisku palca."
            };
        }

        var imageBytes = File.ReadAllBytes(resolvedPath);

        var image = new FingerprintImage(
            imageBytes,
            new FingerprintImageOptions
            {
                Dpi = dpi
            });

        var template = new SourceAFIS.FingerprintTemplate(image);
        var templateBytes = template.ToByteArray();

        if (templateBytes.Length == 0)
        {
            return new FingerprintTemplateExtractionResult
            {
                Success = false,
                Message = "Nie udało się utworzyć szablonu cech odcisku palca."
            };
        }

        return new FingerprintTemplateExtractionResult
        {
            Success = true,
            Message = "Szablon cech odcisku palca został utworzony przez SourceAFIS.",
            TemplateData = templateBytes,
            SourceImagePath = imagePath,
            QualityScore = EstimateTemplateQuality(templateBytes),
            AlgorithmName = "SourceAFIS",
            Dpi = dpi
        };
    }
    catch (Exception ex)
    {
        return new FingerprintTemplateExtractionResult
        {
            Success = false,
            Message = $"Błąd tworzenia szablonu cech odcisku: {ex.Message}"
        };
    }
}