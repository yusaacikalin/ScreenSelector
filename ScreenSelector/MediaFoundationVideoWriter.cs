using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenSelector;

/// <summary>
/// Sends RGB desktop frames to Windows Media Foundation's built-in H.264 encoder.
/// Keeping the encoder in-process means recording does not depend on ffmpeg or
/// another application being installed on the computer.
/// </summary>
internal sealed class MediaFoundationVideoWriter : IDisposable
{
    internal const int FramesPerSecond = 30;
    internal const long FrameDuration = 10_000_000L / FramesPerSecond;
    internal const int AudioSampleRate = 48_000;
    internal const int AudioChannels = 2;
    internal const int AudioBitsPerSample = 16;
    internal const int AudioBlockAlignment = AudioChannels * AudioBitsPerSample / 8;
    internal const int AudioBytesPerVideoFrame = AudioSampleRate * AudioBlockAlignment / FramesPerSecond;

    private readonly int _width;
    private readonly int _height;
    private readonly int _frameBytes;
    private IMFSinkWriter? _sinkWriter;
    private uint _videoStreamIndex;
    private uint _audioStreamIndex;
    private bool _finalized;
    private bool _mediaFoundationStarted;
    private bool _comInitialized;

    internal MediaFoundationVideoWriter(string outputPath, int width, int height)
    {
        _width = width;
        _height = height;
        _frameBytes = checked(width * height * 4);

        var comResult = MediaFoundationNative.CoInitializeEx(IntPtr.Zero, 0);
        if (comResult >= 0)
            _comInitialized = true;
        else if (comResult != MediaFoundationNative.RpcEChangedMode)
            Marshal.ThrowExceptionForHR(comResult);

        try
        {
            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFStartup(
                MediaFoundationNative.MfVersion, MediaFoundationNative.MfStartupFull));
            _mediaFoundationStarted = true;
            InitializeSinkWriter(outputPath);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void WriteFrame(Bitmap frame, long sampleTime)
    {
        ObjectDisposedException.ThrowIf(_sinkWriter is null, this);

        IMFSample? sample = null;
        IMFMediaBuffer? buffer = null;
        BitmapData? bitmapData = null;
        var bufferLocked = false;

        try
        {
            MediaFoundationNative.ThrowIfFailed(
                MediaFoundationNative.MFCreateMemoryBuffer((uint)_frameBytes, out buffer));
            MediaFoundationNative.ThrowIfFailed(buffer.Lock(out var destination, out _, out _));
            bufferLocked = true;

            bitmapData = frame.LockBits(new Rectangle(0, 0, _width, _height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCopyImage(
                destination, _width * 4, bitmapData.Scan0, bitmapData.Stride, _width * 4, _height));

            MediaFoundationNative.ThrowIfFailed(buffer.Unlock());
            bufferLocked = false;
            MediaFoundationNative.ThrowIfFailed(buffer.SetCurrentLength((uint)_frameBytes));

            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateSample(out sample));
            MediaFoundationNative.ThrowIfFailed(sample.AddBuffer(buffer));
            MediaFoundationNative.ThrowIfFailed(sample.SetSampleTime(sampleTime));
            MediaFoundationNative.ThrowIfFailed(sample.SetSampleDuration(FrameDuration));
            MediaFoundationNative.ThrowIfFailed(_sinkWriter.WriteSample(_videoStreamIndex, sample));
        }
        finally
        {
            if (bitmapData is not null) frame.UnlockBits(bitmapData);
            if (bufferLocked && buffer is not null) buffer.Unlock();
            ReleaseComObject(sample);
            ReleaseComObject(buffer);
        }
    }

    internal void WriteAudioFrame(byte[] pcmData, int byteCount, long sampleTime, long sampleDuration)
    {
        ObjectDisposedException.ThrowIf(_sinkWriter is null, this);
        if (byteCount <= 0 || byteCount > pcmData.Length || byteCount % AudioBlockAlignment != 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        IMFSample? sample = null;
        IMFMediaBuffer? buffer = null;
        var bufferLocked = false;

        try
        {
            MediaFoundationNative.ThrowIfFailed(
                MediaFoundationNative.MFCreateMemoryBuffer((uint)byteCount, out buffer));
            MediaFoundationNative.ThrowIfFailed(buffer.Lock(out var destination, out _, out _));
            bufferLocked = true;
            Marshal.Copy(pcmData, 0, destination, byteCount);
            MediaFoundationNative.ThrowIfFailed(buffer.Unlock());
            bufferLocked = false;
            MediaFoundationNative.ThrowIfFailed(buffer.SetCurrentLength((uint)byteCount));

            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateSample(out sample));
            MediaFoundationNative.ThrowIfFailed(sample.AddBuffer(buffer));
            MediaFoundationNative.ThrowIfFailed(sample.SetSampleTime(sampleTime));
            MediaFoundationNative.ThrowIfFailed(sample.SetSampleDuration(sampleDuration));
            MediaFoundationNative.ThrowIfFailed(_sinkWriter.WriteSample(_audioStreamIndex, sample));
        }
        finally
        {
            if (bufferLocked && buffer is not null) buffer.Unlock();
            ReleaseComObject(sample);
            ReleaseComObject(buffer);
        }
    }

    internal void FinalizeFile()
    {
        if (_finalized || _sinkWriter is null) return;
        MediaFoundationNative.ThrowIfFailed(_sinkWriter.FinalizeWriter());
        _finalized = true;
    }

    private void InitializeSinkWriter(string outputPath)
    {
        IMFMediaType? outputType = null;
        IMFMediaType? inputType = null;
        IMFMediaType? audioOutputType = null;
        IMFMediaType? audioInputType = null;

        try
        {
            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateSinkWriterFromURL(
                outputPath, IntPtr.Zero, IntPtr.Zero, out _sinkWriter));

            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateMediaType(out outputType));
            SetGuid(outputType, MediaFoundationGuids.MfMtMajorType, MediaFoundationGuids.MfMediaTypeVideo);
            SetGuid(outputType, MediaFoundationGuids.MfMtSubtype, MediaFoundationGuids.MfVideoFormatH264);
            SetUInt32(outputType, MediaFoundationGuids.MfMtAvgBitrate, CalculateBitrate());
            SetUInt32(outputType, MediaFoundationGuids.MfMtInterlaceMode, 2); // Progressive
            SetUInt64(outputType, MediaFoundationGuids.MfMtFrameSize, PackRatio(_width, _height));
            SetUInt64(outputType, MediaFoundationGuids.MfMtFrameRate, PackRatio(FramesPerSecond, 1));
            SetUInt64(outputType, MediaFoundationGuids.MfMtPixelAspectRatio, PackRatio(1, 1));
            MediaFoundationNative.ThrowIfFailed(_sinkWriter.AddStream(outputType, out _videoStreamIndex));

            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateMediaType(out inputType));
            SetGuid(inputType, MediaFoundationGuids.MfMtMajorType, MediaFoundationGuids.MfMediaTypeVideo);
            SetGuid(inputType, MediaFoundationGuids.MfMtSubtype, MediaFoundationGuids.MfVideoFormatRgb32);
            SetUInt32(inputType, MediaFoundationGuids.MfMtInterlaceMode, 2);
            SetUInt64(inputType, MediaFoundationGuids.MfMtFrameSize, PackRatio(_width, _height));
            SetUInt64(inputType, MediaFoundationGuids.MfMtFrameRate, PackRatio(FramesPerSecond, 1));
            SetUInt64(inputType, MediaFoundationGuids.MfMtPixelAspectRatio, PackRatio(1, 1));
            SetUInt32(inputType, MediaFoundationGuids.MfMtDefaultStride, (uint)(_width * 4));
            MediaFoundationNative.ThrowIfFailed(
                _sinkWriter.SetInputMediaType(_videoStreamIndex, inputType, IntPtr.Zero));

            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateMediaType(out audioOutputType));
            SetGuid(audioOutputType, MediaFoundationGuids.MfMtMajorType, MediaFoundationGuids.MfMediaTypeAudio);
            SetGuid(audioOutputType, MediaFoundationGuids.MfMtSubtype, MediaFoundationGuids.MfAudioFormatAac);
            SetUInt32(audioOutputType, MediaFoundationGuids.MfMtAudioBitsPerSample, AudioBitsPerSample);
            SetUInt32(audioOutputType, MediaFoundationGuids.MfMtAudioSamplesPerSecond, AudioSampleRate);
            SetUInt32(audioOutputType, MediaFoundationGuids.MfMtAudioNumChannels, AudioChannels);
            SetUInt32(audioOutputType, MediaFoundationGuids.MfMtAudioAvgBytesPerSecond, 24_000);
            SetUInt32(audioOutputType, MediaFoundationGuids.MfMtAudioBlockAlignment, 1);
            SetUInt32(audioOutputType, MediaFoundationGuids.MfMtAacPayloadType, 0);
            MediaFoundationNative.ThrowIfFailed(_sinkWriter.AddStream(audioOutputType, out _audioStreamIndex));

            MediaFoundationNative.ThrowIfFailed(MediaFoundationNative.MFCreateMediaType(out audioInputType));
            SetGuid(audioInputType, MediaFoundationGuids.MfMtMajorType, MediaFoundationGuids.MfMediaTypeAudio);
            SetGuid(audioInputType, MediaFoundationGuids.MfMtSubtype, MediaFoundationGuids.MfAudioFormatPcm);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtAudioBitsPerSample, AudioBitsPerSample);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtAudioSamplesPerSecond, AudioSampleRate);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtAudioNumChannels, AudioChannels);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtAudioAvgBytesPerSecond,
                AudioSampleRate * AudioBlockAlignment);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtAudioBlockAlignment, AudioBlockAlignment);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtAllSamplesIndependent, 1);
            SetUInt32(audioInputType, MediaFoundationGuids.MfMtFixedSizeSamples, 1);
            MediaFoundationNative.ThrowIfFailed(
                _sinkWriter.SetInputMediaType(_audioStreamIndex, audioInputType, IntPtr.Zero));
            MediaFoundationNative.ThrowIfFailed(_sinkWriter.BeginWriting());
        }
        finally
        {
            ReleaseComObject(audioInputType);
            ReleaseComObject(audioOutputType);
            ReleaseComObject(inputType);
            ReleaseComObject(outputType);
        }
    }

    private uint CalculateBitrate()
    {
        var bitrate = (long)_width * _height * 4;
        return (uint)Math.Clamp(bitrate, 2_000_000L, 16_000_000L);
    }

    private static ulong PackRatio(int high, int low) => ((ulong)(uint)high << 32) | (uint)low;

    private static void SetGuid(IMFAttributes attributes, Guid key, Guid value) =>
        MediaFoundationNative.ThrowIfFailed(attributes.SetGUID(ref key, ref value));

    private static void SetUInt32(IMFAttributes attributes, Guid key, uint value) =>
        MediaFoundationNative.ThrowIfFailed(attributes.SetUINT32(ref key, value));

    private static void SetUInt64(IMFAttributes attributes, Guid key, ulong value) =>
        MediaFoundationNative.ThrowIfFailed(attributes.SetUINT64(ref key, value));

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value);
    }

    public void Dispose()
    {
        ReleaseComObject(_sinkWriter);
        _sinkWriter = null;

        if (_mediaFoundationStarted)
        {
            MediaFoundationNative.MFShutdown();
            _mediaFoundationStarted = false;
        }

        if (_comInitialized)
        {
            MediaFoundationNative.CoUninitialize();
            _comInitialized = false;
        }
    }
}

internal static class MediaFoundationGuids
{
    internal static readonly Guid MfMediaTypeVideo = new("73646976-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MfMediaTypeAudio = new("73647561-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MfVideoFormatH264 = new("34363248-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MfVideoFormatRgb32 = new("00000016-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MfAudioFormatPcm = new("00000001-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MfAudioFormatAac = new("00001610-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MfMtMajorType = new("48EBA18E-F8C9-4687-BF11-0A74C9F96A8F");
    internal static readonly Guid MfMtSubtype = new("F7E34C9A-42E8-4714-B74B-CB29D72C35E5");
    internal static readonly Guid MfMtFrameSize = new("1652C33D-D6B2-4012-B834-72030849A37D");
    internal static readonly Guid MfMtFrameRate = new("C459A2E8-3D2C-4E44-B132-FEE5156C7BB0");
    internal static readonly Guid MfMtPixelAspectRatio = new("C6376A1E-8D0A-4027-BE45-6D9A0AD39BB6");
    internal static readonly Guid MfMtInterlaceMode = new("E2724BB8-E676-4806-B4B2-A8D6EFB44CCD");
    internal static readonly Guid MfMtAvgBitrate = new("20332624-FB0D-4D9E-BD0D-CBF6786C102E");
    internal static readonly Guid MfMtDefaultStride = new("644B4E48-1E02-4516-B0EB-C01CA9D49AC6");
    internal static readonly Guid MfMtAllSamplesIndependent = new("C9173739-5E56-461C-B713-46FB995CB95F");
    internal static readonly Guid MfMtFixedSizeSamples = new("B8EBEFAF-B718-4E04-B0A9-116775E3321B");
    internal static readonly Guid MfMtAudioNumChannels = new("37E48BF5-645E-4C5B-89DE-ADA9E29B696A");
    internal static readonly Guid MfMtAudioSamplesPerSecond = new("5FAEEAE7-0290-4C31-9E8A-C534F68D9DBA");
    internal static readonly Guid MfMtAudioAvgBytesPerSecond = new("1AAB75C8-CFEF-451C-AB95-AC034B8E1731");
    internal static readonly Guid MfMtAudioBlockAlignment = new("322DE230-9EEB-43BD-AB7A-FF412251541D");
    internal static readonly Guid MfMtAudioBitsPerSample = new("F2DEB57F-40FA-4764-AA33-ED4F2D1FF669");
    internal static readonly Guid MfMtAacPayloadType = new("BFBABE79-7434-4D1C-94F0-72A3B9E17188");
}

internal static class MediaFoundationNative
{
    internal const int MfVersion = 0x00020070;
    internal const int MfStartupFull = 0;
    internal const int RpcEChangedMode = unchecked((int)0x80010106);

    [DllImport("ole32.dll")]
    internal static extern int CoInitializeEx(IntPtr reserved, uint coInit);

    [DllImport("ole32.dll")]
    internal static extern void CoUninitialize();

    [DllImport("mfplat.dll")]
    internal static extern int MFStartup(int version, int flags);

    [DllImport("mfplat.dll")]
    internal static extern int MFShutdown();

    [DllImport("mfplat.dll")]
    internal static extern int MFCreateMediaType(out IMFMediaType mediaType);

    [DllImport("mfplat.dll")]
    internal static extern int MFCreateSample(out IMFSample sample);

    [DllImport("mfplat.dll")]
    internal static extern int MFCreateMemoryBuffer(uint maximumLength, out IMFMediaBuffer buffer);

    [DllImport("mfplat.dll")]
    internal static extern int MFCopyImage(IntPtr destination, int destinationStride, IntPtr source,
        int sourceStride, int widthInBytes, int lines);

    [DllImport("mfreadwrite.dll", CharSet = CharSet.Unicode)]
    internal static extern int MFCreateSinkWriterFromURL(string outputUrl, IntPtr byteStream,
        IntPtr attributes, out IMFSinkWriter sinkWriter);

    internal static void ThrowIfFailed(int result)
    {
        if (result < 0) Marshal.ThrowExceptionForHR(result);
    }
}

[ComImport, Guid("2CD2D921-C447-44A7-A13C-4ADABFC247E3"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFAttributes
{
    [PreserveSig] int GetItem(ref Guid key, IntPtr value);
    [PreserveSig] int GetItemType(ref Guid key, out int type);
    [PreserveSig] int CompareItem(ref Guid key, IntPtr value, out int result);
    [PreserveSig] int Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes theirs, int matchType, out int result);
    [PreserveSig] int GetUINT32(ref Guid key, out uint value);
    [PreserveSig] int GetUINT64(ref Guid key, out ulong value);
    [PreserveSig] int GetDouble(ref Guid key, out double value);
    [PreserveSig] int GetGUID(ref Guid key, out Guid value);
    [PreserveSig] int GetStringLength(ref Guid key, out uint length);
    [PreserveSig] int GetString(ref Guid key, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value,
        uint bufferSize, IntPtr length);
    [PreserveSig] int GetAllocatedString(ref Guid key, out IntPtr value, out uint length);
    [PreserveSig] int GetBlobSize(ref Guid key, out uint size);
    [PreserveSig] int GetBlob(ref Guid key, IntPtr buffer, uint bufferSize, IntPtr blobSize);
    [PreserveSig] int GetAllocatedBlob(ref Guid key, out IntPtr buffer, out uint size);
    [PreserveSig] int GetUnknown(ref Guid key, ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object value);
    [PreserveSig] int SetItem(ref Guid key, IntPtr value);
    [PreserveSig] int DeleteItem(ref Guid key);
    [PreserveSig] int DeleteAllItems();
    [PreserveSig] int SetUINT32(ref Guid key, uint value);
    [PreserveSig] int SetUINT64(ref Guid key, ulong value);
    [PreserveSig] int SetDouble(ref Guid key, double value);
    [PreserveSig] int SetGUID(ref Guid key, ref Guid value);
    [PreserveSig] int SetString(ref Guid key, [MarshalAs(UnmanagedType.LPWStr)] string value);
    [PreserveSig] int SetBlob(ref Guid key, IntPtr buffer, uint bufferSize);
    [PreserveSig] int SetUnknown(ref Guid key, [MarshalAs(UnmanagedType.IUnknown)] object value);
    [PreserveSig] int LockStore();
    [PreserveSig] int UnlockStore();
    [PreserveSig] int GetCount(out uint itemCount);
    [PreserveSig] int GetItemByIndex(uint index, out Guid key, IntPtr value);
    [PreserveSig] int CopyAllItems([MarshalAs(UnmanagedType.Interface)] IMFAttributes destination);
}

[ComImport, Guid("44AE0FA8-EA31-4109-8D2E-4CAE4997C555"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaType : IMFAttributes
{
    [PreserveSig] int GetMajorType(out Guid majorType);
    [PreserveSig] int IsCompressedFormat(out int compressed);
    [PreserveSig] int IsEqual([MarshalAs(UnmanagedType.Interface)] IMFMediaType mediaType, out uint flags);
    [PreserveSig] int GetRepresentation(Guid representation, out IntPtr value);
    [PreserveSig] int FreeRepresentation(Guid representation, IntPtr value);
}

[ComImport, Guid("045FA593-8799-42B8-BC8D-8968C6453507"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaBuffer
{
    [PreserveSig] int Lock(out IntPtr buffer, out uint maximumLength, out uint currentLength);
    [PreserveSig] int Unlock();
    [PreserveSig] int GetCurrentLength(out uint currentLength);
    [PreserveSig] int SetCurrentLength(uint currentLength);
    [PreserveSig] int GetMaxLength(out uint maximumLength);
}

[ComImport, Guid("C40A00F2-B93A-4D80-AE8C-5A1C634F58E4"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSample
{
    // COM interface inheritance must be flattened here so the IMFSample methods
    // keep their native vtable positions after IMFAttributes' 30 methods.
    [PreserveSig] int GetItem(ref Guid key, IntPtr value);
    [PreserveSig] int GetItemType(ref Guid key, out int type);
    [PreserveSig] int CompareItem(ref Guid key, IntPtr value, out int result);
    [PreserveSig] int Compare(IntPtr theirs, int matchType, out int result);
    [PreserveSig] int GetUINT32(ref Guid key, out uint value);
    [PreserveSig] int GetUINT64(ref Guid key, out ulong value);
    [PreserveSig] int GetDouble(ref Guid key, out double value);
    [PreserveSig] int GetGUID(ref Guid key, out Guid value);
    [PreserveSig] int GetStringLength(ref Guid key, out uint length);
    [PreserveSig] int GetString(ref Guid key, IntPtr value, uint bufferSize, IntPtr length);
    [PreserveSig] int GetAllocatedString(ref Guid key, out IntPtr value, out uint length);
    [PreserveSig] int GetBlobSize(ref Guid key, out uint size);
    [PreserveSig] int GetBlob(ref Guid key, IntPtr buffer, uint bufferSize, IntPtr blobSize);
    [PreserveSig] int GetAllocatedBlob(ref Guid key, out IntPtr buffer, out uint size);
    [PreserveSig] int GetUnknown(ref Guid key, ref Guid interfaceId, out IntPtr value);
    [PreserveSig] int SetItem(ref Guid key, IntPtr value);
    [PreserveSig] int DeleteItem(ref Guid key);
    [PreserveSig] int DeleteAllItems();
    [PreserveSig] int SetUINT32(ref Guid key, uint value);
    [PreserveSig] int SetUINT64(ref Guid key, ulong value);
    [PreserveSig] int SetDouble(ref Guid key, double value);
    [PreserveSig] int SetGUID(ref Guid key, ref Guid value);
    [PreserveSig] int SetString(ref Guid key, IntPtr value);
    [PreserveSig] int SetBlob(ref Guid key, IntPtr buffer, uint bufferSize);
    [PreserveSig] int SetUnknown(ref Guid key, IntPtr value);
    [PreserveSig] int LockStore();
    [PreserveSig] int UnlockStore();
    [PreserveSig] int GetCount(out uint itemCount);
    [PreserveSig] int GetItemByIndex(uint index, out Guid key, IntPtr value);
    [PreserveSig] int CopyAllItems(IntPtr destination);
    [PreserveSig] int GetSampleFlags(out uint sampleFlags);
    [PreserveSig] int SetSampleFlags(uint sampleFlags);
    [PreserveSig] int GetSampleTime(out long sampleTime);
    [PreserveSig] int SetSampleTime(long sampleTime);
    [PreserveSig] int GetSampleDuration(out long sampleDuration);
    [PreserveSig] int SetSampleDuration(long sampleDuration);
    [PreserveSig] int GetBufferCount(out uint bufferCount);
    [PreserveSig] int GetBufferByIndex(uint index, out IMFMediaBuffer buffer);
    [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer buffer);
    [PreserveSig] int AddBuffer(IMFMediaBuffer buffer);
    [PreserveSig] int RemoveBufferByIndex(uint index);
    [PreserveSig] int RemoveAllBuffers();
    [PreserveSig] int GetTotalLength(out uint totalLength);
    [PreserveSig] int CopyToBuffer(IMFMediaBuffer buffer);
}

[ComImport, Guid("3137F1CD-FE5E-4805-A5D8-FB477448CB3D"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSinkWriter
{
    [PreserveSig] int AddStream(IMFMediaType targetMediaType, out uint streamIndex);
    [PreserveSig] int SetInputMediaType(uint streamIndex, IMFMediaType inputMediaType, IntPtr encodingParameters);
    [PreserveSig] int BeginWriting();
    [PreserveSig] int WriteSample(uint streamIndex, IMFSample sample);
    [PreserveSig] int SendStreamTick(uint streamIndex, long timestamp);
    [PreserveSig] int PlaceMarker(uint streamIndex, IntPtr context);
    [PreserveSig] int NotifyEndOfSegment(uint streamIndex);
    [PreserveSig] int Flush(uint streamIndex);
    [PreserveSig] int FinalizeWriter();
    [PreserveSig] int GetServiceForStream(uint streamIndex, ref Guid service, ref Guid interfaceId, out IntPtr value);
    [PreserveSig] int GetStatistics(uint streamIndex, IntPtr statistics);
}
