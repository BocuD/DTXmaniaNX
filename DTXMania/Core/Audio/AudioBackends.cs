namespace DTXMania.Core.Audio;

internal static class AudioBackends
{
    public static AudioBackend Fallback => OperatingSystem.IsWindows() ? AudioBackend.WasapiShared : AudioBackend.Bass;

    //DirectSound, ASIO and WASAPI are Windows only
    public static AudioBackend[] Supported { get; } = OperatingSystem.IsWindows()
        ? [AudioBackend.DirectSound, AudioBackend.Asio, AudioBackend.WasapiExclusive, AudioBackend.WasapiShared, AudioBackend.Bass]
        : [AudioBackend.Bass];

    public static AudioBackend Resolve(AudioBackend backend) => Supported.Contains(backend) ? backend : Fallback;
}
