using System.Diagnostics;
using System.Drawing.Imaging;
using NAudio.Wave;

namespace ScreenSelector;

internal sealed class ScreenRecordingService : IDisposable
{
    private readonly Rectangle _captureArea;
    private readonly string _outputPath;
    private readonly CancellationTokenSource _stopSource = new();
    private readonly TaskCompletionSource<bool> _started =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenRegistration _externalCancellation;
    private Task? _recordingTask;
    private bool _disposed;

    internal ScreenRecordingService(Rectangle selectedArea, string outputPath)
    {
        _captureArea = NormalizeCaptureArea(selectedArea);
        _outputPath = outputPath;
    }

    internal Rectangle CaptureArea => _captureArea;
    internal string OutputPath => _outputPath;
    internal Task Completion => _recordingTask ?? Task.CompletedTask;

    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_recordingTask is not null)
            throw new InvalidOperationException("Ekran kaydı zaten başlatıldı.");

        _externalCancellation = cancellationToken.Register(_stopSource.Cancel);
        _recordingTask = Task.Run(CaptureLoop);
        await _started.Task.WaitAsync(cancellationToken);
    }

    internal async Task StopAsync()
    {
        if (_recordingTask is null) return;
        _stopSource.Cancel();
        await _recordingTask;
    }

    internal static string CreateOutputPath()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures))
        {
            pictures = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures");
        }

        var recordingFolder = Path.Combine(pictures, "Yuce Ekran Kaydı");
        Directory.CreateDirectory(recordingFolder);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var path = Path.Combine(recordingFolder, $"Ekran Kaydı_{timestamp}.mp4");
        for (var suffix = 2; File.Exists(path); suffix++)
            path = Path.Combine(recordingFolder, $"Ekran Kaydı_{timestamp}_{suffix}.mp4");
        return path;
    }

    private void CaptureLoop()
    {
        try
        {
            using var audioCapture = new WasapiLoopbackCapture
            {
                WaveFormat = new WaveFormat(MediaFoundationVideoWriter.AudioSampleRate,
                    MediaFoundationVideoWriter.AudioBitsPerSample,
                    MediaFoundationVideoWriter.AudioChannels)
            };
            var audioBuffer = new BufferedWaveProvider(audioCapture.WaveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            Exception? audioCaptureError = null;
            audioCapture.DataAvailable += (_, eventArgs) =>
                audioBuffer.AddSamples(eventArgs.Buffer, 0, eventArgs.BytesRecorded);
            audioCapture.RecordingStopped += (_, eventArgs) =>
                Volatile.Write(ref audioCaptureError, eventArgs.Exception);

            using var writer = new MediaFoundationVideoWriter(
                _outputPath, _captureArea.Width, _captureArea.Height);
            using var frame = new Bitmap(_captureArea.Width, _captureArea.Height,
                PixelFormat.Format32bppRgb);
            using var graphics = Graphics.FromImage(frame);
            var audioFrame = new byte[MediaFoundationVideoWriter.AudioBytesPerVideoFrame];

            audioCapture.StartRecording();
            _started.TrySetResult(true);
            var clock = Stopwatch.StartNew();
            long frameIndex = 0;

            do
            {
                var captureError = Volatile.Read(ref audioCaptureError);
                if (captureError is not null)
                    throw new InvalidOperationException("Bilgisayar sesi yakalama işlemi durdu.", captureError);

                graphics.CopyFromScreen(_captureArea.Location, Point.Empty, _captureArea.Size,
                    CopyPixelOperation.SourceCopy);
                DrawCursor(graphics);

                var sampleTime = frameIndex * MediaFoundationVideoWriter.FrameDuration;
                writer.WriteFrame(frame, sampleTime);
                audioBuffer.Read(audioFrame, 0, audioFrame.Length);
                writer.WriteAudioFrame(audioFrame, audioFrame.Length, sampleTime,
                    MediaFoundationVideoWriter.FrameDuration);
                frameIndex++;

                var nextFrameAt = TimeSpan.FromTicks(
                    frameIndex * MediaFoundationVideoWriter.FrameDuration);
                var wait = nextFrameAt - clock.Elapsed;
                if (wait > TimeSpan.Zero)
                    _stopSource.Token.WaitHandle.WaitOne(wait);
            }
            while (!_stopSource.IsCancellationRequested);

            audioCapture.StopRecording();
            writer.FinalizeFile();
        }
        catch (Exception ex)
        {
            _started.TrySetException(ex);
            throw;
        }
    }

    private void DrawCursor(Graphics graphics)
    {
        var cursorInfo = new NativeMethods.CursorInfo
        {
            Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.CursorInfo>()
        };
        if (!NativeMethods.GetCursorInfo(ref cursorInfo) ||
            (cursorInfo.Flags & NativeMethods.CursorShowing) == 0 ||
            cursorInfo.CursorHandle == IntPtr.Zero ||
            !_captureArea.Contains(cursorInfo.ScreenPosition))
            return;

        if (!NativeMethods.GetIconInfo(cursorInfo.CursorHandle, out var iconInfo)) return;

        try
        {
            var x = cursorInfo.ScreenPosition.X - _captureArea.Left - (int)iconInfo.XHotspot;
            var y = cursorInfo.ScreenPosition.Y - _captureArea.Top - (int)iconInfo.YHotspot;
            var deviceContext = graphics.GetHdc();
            try
            {
                NativeMethods.DrawIconEx(deviceContext, x, y, cursorInfo.CursorHandle,
                    0, 0, 0, IntPtr.Zero, NativeMethods.DrawIconNormal);
            }
            finally
            {
                graphics.ReleaseHdc(deviceContext);
            }
        }
        finally
        {
            if (iconInfo.MaskBitmap != IntPtr.Zero) NativeMethods.DeleteObject(iconInfo.MaskBitmap);
            if (iconInfo.ColorBitmap != IntPtr.Zero) NativeMethods.DeleteObject(iconInfo.ColorBitmap);
        }
    }

    private static Rectangle NormalizeCaptureArea(Rectangle selectedArea)
    {
        var area = Rectangle.Intersect(selectedArea, SystemInformation.VirtualScreen);
        if ((area.Width & 1) != 0) area.Width--;
        if ((area.Height & 1) != 0) area.Height--;

        if (area.Width < 16 || area.Height < 16)
            throw new InvalidOperationException("Video kaydı için en az 16 × 16 piksellik bir alan seçin.");
        return area;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stopSource.Cancel();
        try
        {
            _recordingTask?.GetAwaiter().GetResult();
        }
        catch
        {
            // StopAsync/StartAsync reports recording errors to the UI. Dispose
            // only guarantees that the encoder released the output file.
        }
        _externalCancellation.Dispose();
        _stopSource.Dispose();
    }
}
