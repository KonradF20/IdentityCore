using Microsoft.ML.Data;

namespace IdentityCore.App.Models;

public class FaceOnnxInput
{
    [ColumnName("input_1")]
    [VectorType(1, 112, 112, 3)]
    public float[] Data { get; set; } = [];
}