namespace IdentityCore.App.Models;

public class BiometricDecisionSettings
{
    public double FaceMatchThreshold { get; set; } = 60.0;

    public double FaceMinimumMargin { get; set; } = 15.0;

    public double FingerprintMatchThreshold { get; set; } = 85.0;

    public double FingerprintMinimumMargin { get; set; } = 15.0;
}
