private static double CalculateSimilarityPercentage(float[] first, float[] second)
{
    if (first.Length == 0 || second.Length == 0 || first.Length != second.Length)
    {
        return 0;
    }

    double dot = 0;
    double normFirst = 0;
    double normSecond = 0;

    for (var i = 0; i < first.Length; i++)
    {
        dot += first[i] * second[i];
        normFirst += first[i] * first[i];
        normSecond += second[i] * second[i];
    }

    if (normFirst == 0 || normSecond == 0)
    {
        return 0;
    }

    var cosine = dot / (Math.Sqrt(normFirst) * Math.Sqrt(normSecond));
    var percentage = cosine * 100.0;

    return Math.Clamp(percentage, 0, 100);
}