public Mat? ExtractAndPrepareFace(Mat originalFrame, out double qualityScore)
{
    qualityScore = 0;

    if (originalFrame.Empty())
    {
        return null;
    }

    var faceRect = DetectFrontalFace(originalFrame);

    if (faceRect.Width == 0 || faceRect.Height == 0)
    {
        return null;
    }

    var expandedFaceRect = ExpandToSquareRect(faceRect, originalFrame.Width, originalFrame.Height, 0.28);

    using var faceCrop = new Mat(originalFrame, expandedFaceRect).Clone();

    if (!IsCropGeometryUsable(faceCrop))
    {
        return null;
    }

    qualityScore = EstimateImageQuality(faceCrop);

    var resized = new Mat();
    Cv2.Resize(faceCrop, resized, new Size(TargetSize, TargetSize), 0, 0, InterpolationFlags.Area);

    if (!ContainsFrontalFace(resized))
    {
        resized.Dispose();
        return null;
    }

    return resized;
}