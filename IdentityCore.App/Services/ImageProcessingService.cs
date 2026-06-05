using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace IdentityCore.App.Services;

public class ImageProcessingService
{
    private const int TargetSize = 112;
    private const string FrontalCascadeFileName = "haarcascade_frontalface_default.xml";

    /// <summary>
    /// Przygotowuje próbkę twarzy do ArcFace. Na obecnym etapie projekt akceptuje tylko twarz
    /// frontalną lub bardzo lekki półprofil. Pełne profile i błędne cropy są odrzucane, żeby
    /// do modelu nie trafiały próbki typu szyja/ubranie/tło.
    /// </summary>
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

    private Rect DetectFrontalFace(Mat image)
    {
        var candidates = DetectWithCascade(
                image,
                ResolveCascadePath(FrontalCascadeFileName),
                mirrored: false,
                minNeighbors: 5,
                minSize: new Size(60, 60))
            .Where(rect => IsDetectedFaceGeometryUsable(rect, image.Width, image.Height))
            .ToList();

        if (candidates.Count == 0)
        {
            return new Rect(0, 0, 0, 0);
        }

        return candidates
            .OrderByDescending(rect => rect.Width * rect.Height)
            .First();
    }

    private IEnumerable<Rect> DetectWithCascade(
        Mat image,
        string cascadePath,
        bool mirrored,
        int minNeighbors,
        Size minSize)
    {
        if (!File.Exists(cascadePath))
        {
            yield break;
        }

        using var cascade = new CascadeClassifier(cascadePath);

        if (cascade.Empty())
        {
            yield break;
        }

        using var workingImage = mirrored ? new Mat() : image.Clone();

        if (mirrored)
        {
            Cv2.Flip(image, workingImage, FlipMode.Y);
        }

        using var gray = new Mat();

        if (workingImage.Channels() == 1)
        {
            workingImage.CopyTo(gray);
        }
        else
        {
            Cv2.CvtColor(workingImage, gray, ColorConversionCodes.BGR2GRAY);
        }

        Cv2.EqualizeHist(gray, gray);

        var faces = cascade.DetectMultiScale(
            image: gray,
            scaleFactor: 1.08,
            minNeighbors: minNeighbors,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: minSize);

        foreach (var face in faces)
        {
            if (!mirrored)
            {
                yield return face;
            }
            else
            {
                var correctedX = image.Width - face.X - face.Width;
                yield return new Rect(correctedX, face.Y, face.Width, face.Height);
            }
        }
    }

    private static Rect ExpandToSquareRect(Rect rect, int maxWidth, int maxHeight, double marginScale)
    {
        var centerX = rect.X + rect.Width / 2.0;
        var centerY = rect.Y + rect.Height / 2.0;

        var side = (int)Math.Round(Math.Max(rect.Width, rect.Height) * (1.0 + marginScale * 2.0));
        side = Math.Max(side, Math.Max(rect.Width, rect.Height));
        side = Math.Min(side, Math.Min(maxWidth, maxHeight));

        var x = (int)Math.Round(centerX - side / 2.0);
        var y = (int)Math.Round(centerY - side / 2.0);

        x = Math.Clamp(x, 0, Math.Max(0, maxWidth - side));
        y = Math.Clamp(y, 0, Math.Max(0, maxHeight - side));

        return new Rect(x, y, side, side);
    }

    private static bool IsDetectedFaceGeometryUsable(Rect rect, int imageWidth, int imageHeight)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return false;
        }

        var aspectRatio = rect.Width / (double)rect.Height;
        if (aspectRatio < 0.65 || aspectRatio > 1.45)
        {
            return false;
        }

        var areaRatio = rect.Width * rect.Height / (double)(imageWidth * imageHeight);
        if (areaRatio < 0.015 || areaRatio > 0.80)
        {
            return false;
        }

        var centerX = (rect.X + rect.Width / 2.0) / imageWidth;
        var centerY = (rect.Y + rect.Height / 2.0) / imageHeight;

        return centerX is >= 0.12 and <= 0.88 && centerY is >= 0.12 and <= 0.82;
    }

    private static bool IsCropGeometryUsable(Mat faceCrop)
    {
        if (faceCrop.Empty() || faceCrop.Width < 70 || faceCrop.Height < 70)
        {
            return false;
        }

        var aspectRatio = faceCrop.Width / (double)faceCrop.Height;
        return aspectRatio is >= 0.85 and <= 1.15;
    }

    private bool ContainsFrontalFace(Mat preparedFace)
    {
        var detections = DetectWithCascade(
                preparedFace,
                ResolveCascadePath(FrontalCascadeFileName),
                mirrored: false,
                minNeighbors: 4,
                minSize: new Size(35, 35))
            .Where(rect => IsPreparedFaceDetectionUsable(rect, preparedFace.Width, preparedFace.Height))
            .ToList();

        return detections.Count > 0;
    }

    private static bool IsPreparedFaceDetectionUsable(Rect rect, int imageWidth, int imageHeight)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return false;
        }

        var aspectRatio = rect.Width / (double)rect.Height;
        if (aspectRatio < 0.65 || aspectRatio > 1.45)
        {
            return false;
        }

        var widthRatio = rect.Width / (double)imageWidth;
        var heightRatio = rect.Height / (double)imageHeight;

        if (widthRatio < 0.30 || widthRatio > 0.95 || heightRatio < 0.30 || heightRatio > 0.95)
        {
            return false;
        }

        var centerX = (rect.X + rect.Width / 2.0) / imageWidth;
        var centerY = (rect.Y + rect.Height / 2.0) / imageHeight;

        return centerX is >= 0.25 and <= 0.75 && centerY is >= 0.25 and <= 0.75;
    }

    private static double EstimateImageQuality(Mat faceCrop)
    {
        using var gray = new Mat();

        if (faceCrop.Channels() == 1)
        {
            faceCrop.CopyTo(gray);
        }
        else
        {
            Cv2.CvtColor(faceCrop, gray, ColorConversionCodes.BGR2GRAY);
        }

        using var laplacian = new Mat();
        Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

        Cv2.MeanStdDev(laplacian, out _, out var stddev);

        var variance = stddev.Val0 * stddev.Val0;

        return Math.Clamp(variance / 10.0, 0, 100);
    }

    private static string ResolveCascadePath(string cascadeFileName)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var outputCandidate = Path.Combine(baseDirectory, "Assets", "Cascades", cascadeFileName);

        if (File.Exists(outputCandidate))
        {
            return outputCandidate;
        }

        var directory = new DirectoryInfo(baseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Assets", "Cascades", cascadeFileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return outputCandidate;
    }
}
