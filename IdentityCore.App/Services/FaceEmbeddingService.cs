using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML;
using OpenCvSharp;
using IdentityCore.App.Models;

namespace IdentityCore.App.Services;

public class FaceEmbeddingService
{
    private const int ImageSize = 112;
    private const string ModelFileName = "arcface.onnx";

    private readonly MLContext _mlContext = new(seed: 1);
    private readonly PredictionEngine<FaceOnnxInput, FaceOnnxOutput> _predictionEngine;
    private readonly object _predictionLock = new();

    public FaceEmbeddingService()
    {
        var modelPath = ResolveModelPath();

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono modelu arcface.onnx. Sprawdź folder MLModels i ustawienie Copy to Output Directory.",
                modelPath);
        }

        var emptyData = _mlContext.Data.LoadFromEnumerable(new List<FaceOnnxInput>());

        var pipeline = _mlContext.Transforms.ApplyOnnxModel(
            outputColumnNames: new[] { "embedding" },
            inputColumnNames: new[] { "input_1" },
            modelFile: modelPath);

        var model = pipeline.Fit(emptyData);

        _predictionEngine = _mlContext.Model.CreatePredictionEngine<FaceOnnxInput, FaceOnnxOutput>(model);
    }

    public float[] GenerateEmbeddingFromMat(Mat preparedFaceBgr)
    {
        var input = CreateOnnxInput(preparedFaceBgr);

        FaceOnnxOutput output;
        lock (_predictionLock)
        {
            output = _predictionEngine.Predict(input);
        }

        return NormalizeEmbedding(output.Embedding);
    }

    private static FaceOnnxInput CreateOnnxInput(Mat preparedFaceBgr)
    {
        using var preparedFaceRgb = new Mat();
        Cv2.CvtColor(preparedFaceBgr, preparedFaceRgb, ColorConversionCodes.BGR2RGB);

        // WRACAMY DO NHWC! (Kanały na końcu)
        var data = new float[1 * ImageSize * ImageSize * 3];
        int pixelIndex = 0;

        for (var y = 0; y < ImageSize; y++)
        {
            for (var x = 0; x < ImageSize; x++)
            {
                var pixel = preparedFaceRgb.At<Vec3b>(y, x);

                // Zapisujemy R, potem G, potem B dla każdego piksela po kolei
                data[pixelIndex++] = (pixel.Item0 - 127.5f) / 128.0f; // R
                data[pixelIndex++] = (pixel.Item1 - 127.5f) / 128.0f; // G
                data[pixelIndex++] = (pixel.Item2 - 127.5f) / 128.0f; // B
            }
        }

        return new FaceOnnxInput
        {
            Data = data
        };
    }

    private static float[] NormalizeEmbedding(float[] embedding)
    {
        if (embedding.Length == 0)
        {
            return embedding;
        }

        var sum = 0.0;

        foreach (var value in embedding)
        {
            sum += value * value;
        }

        var norm = Math.Sqrt(sum);

        if (norm == 0)
        {
            return embedding;
        }

        var normalized = new float[embedding.Length];

        for (var i = 0; i < embedding.Length; i++)
        {
            normalized[i] = (float)(embedding[i] / norm);
        }

        return normalized;
    }

    private static string ResolveModelPath()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var outputCandidate = Path.Combine(baseDirectory, "MLModels", ModelFileName);

        if (File.Exists(outputCandidate))
        {
            return outputCandidate;
        }

        var directory = new DirectoryInfo(baseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "MLModels", ModelFileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return outputCandidate;
    }
}