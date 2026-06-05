using System;

namespace IdentityCore.App.Models;

public class FaceTemplate
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    public string SourceImagePath { get; set; } = string.Empty;

    public string SourceType { get; set; } = "Plik";

    public string EmbeddingJson { get; set; } = string.Empty;

    public double QualityScore { get; set; }

    public string ModelName { get; set; } = "arcface.onnx";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}