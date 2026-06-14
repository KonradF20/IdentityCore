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