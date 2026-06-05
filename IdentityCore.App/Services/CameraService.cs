using System;
using System.IO;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace IdentityCore.App.Services;

public class CameraService : IDisposable
{
    private VideoCapture? _capture;
    private Mat? _lastFrame;

    public bool IsRunning => _capture != null && _capture.IsOpened();

    public void Start(int cameraIndex = 0)
    {
        Stop();

        _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);

        if (!_capture.IsOpened())
        {
            throw new InvalidOperationException("Nie udało się uruchomić kamery. Sprawdź, czy kamera jest podłączona i nie jest używana przez inną aplikację.");
        }

        _capture.Set(VideoCaptureProperties.FrameWidth, 640);
        _capture.Set(VideoCaptureProperties.FrameHeight, 480);
        _capture.Set(VideoCaptureProperties.Fps, 30);
    }

    public BitmapSource? CaptureFrame()
    {
        if (_capture == null || !_capture.IsOpened())
        {
            return null;
        }

        using var frame = new Mat();

        if (!_capture.Read(frame) || frame.Empty())
        {
            return null;
        }

        Cv2.Flip(frame, frame, FlipMode.Y);

        _lastFrame?.Dispose();
        _lastFrame = frame.Clone();

        var bitmapSource = BitmapSourceConverter.ToBitmapSource(frame);
        bitmapSource.Freeze();

        return bitmapSource;
    }

    public string SaveCurrentFrame(string filePath)
    {
        if (_lastFrame == null || _lastFrame.Empty())
        {
            throw new InvalidOperationException("Brak aktualnej klatki do zapisania.");
        }

        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Cv2.ImWrite(filePath, _lastFrame);

        return filePath;
    }

    public void Stop()
    {
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;

        _lastFrame?.Dispose();
        _lastFrame = null;
    }

    public void Dispose()
    {
        Stop();
    }
}