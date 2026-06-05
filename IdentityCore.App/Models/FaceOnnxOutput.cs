using Microsoft.ML.Data;

namespace IdentityCore.App.Models;

public class FaceOnnxOutput
{
    [ColumnName("embedding")]
    [VectorType(1, 512)]
    public float[] Embedding { get; set; } = new float[512];
}
