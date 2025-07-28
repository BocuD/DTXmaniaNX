using System.Diagnostics;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace DTXMania.Core.Video;

public class FFmpegCore
{
    public static bool IsInitialized { get; private set; } = false;
    
    public static void Initialize()
    {
        try
        {
            DynamicallyLoadedBindings.LibrariesPath = "FFmpeg/bin/x64";
            DynamicallyLoadedBindings.ThrowErrorIfFunctionNotFound = true;
            DynamicallyLoadedBindings.Initialize();
            
            Trace.TraceInformation("Initializing FFmpeg");
            Trace.TraceInformation($"FFmpeg version info: {ffmpeg.av_version_info()}");
            Trace.TraceInformation($"LIBAVFORMAT Version: {ffmpeg.LIBAVFORMAT_VERSION_MAJOR}.{ffmpeg.LIBAVFORMAT_VERSION_MINOR}");
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            Trace.TraceError("FFmpeg initialization failed: " + ex.Message);
            IsInitialized = false;
        }
    }

    //take in error code, call ffmpeg.av_strerror to get the error message
    public static unsafe string AV_StrError(int sendErr)
    {
        if (sendErr == 0)
            return "No error";

        byte[] buffer = new byte[1024];
        
        //get byte* from buffer
        fixed (byte* pBuffer = buffer)
        {
            //call ffmpeg.av_strerror
            ffmpeg.av_strerror(sendErr, pBuffer, (ulong)buffer.Length);
            return System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        }
    }
}