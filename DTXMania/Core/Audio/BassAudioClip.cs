using System.Diagnostics;
using System.Runtime.InteropServices;
using Un4seen.Bass;

namespace DTXMania.Core.Audio;

/// <summary>
/// One file, decoded as few times as it can be. A short sound is decoded once into memory and every voice
/// reads that; a long one is left on disk and decoded per voice, since it only ever needs one.
/// </summary>
internal sealed class BassAudioClip : IAudioClip
{
    //decoded, not the file size: a small compressed file can come to a great deal of PCM. About 45
    //seconds of stereo float, which is far longer than a chip and far shorter than a song
    private const long ShareLimit = 16 * 1024 * 1024;

    private readonly BassAudioDevice device;
    private readonly string path;
    private readonly List<IAudioVoice> voices = [];

    //the decoded file, pinned because each voice creates a stream over the memory
    private readonly byte[]? image;
    private GCHandle pin;

    public string VoiceKind { get; }

    public long LengthMs { get; private set; }

    public BassAudioClip(BassAudioDevice device, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        this.device = device;
        this.path = path;

        //a file BASS cannot read well is decoded whether or not it would be shared
        image = PcmDecoder.DecodeIfNeeded(path) ?? (Shareable() ? PcmDecoder.DecodeToFloat(path, ShareLimit) : null);

        if (image != null)
        {
            pin = GCHandle.Alloc(image, GCHandleType.Pinned);
        }

        VoiceKind = image != null ? "shared" : "stream";
    }

    public IAudioVoice? CreateVoice()
    {
        BassStreamVoice? voice = image != null
            ? BassStreamVoice.Create(device, pin.AddrOfPinnedObject(), image.Length, path)
            : BassStreamVoice.Create(device, path);

        if (voice == null)
        {
            return null;
        }

        LengthMs = voice.LengthMs;
        voices.Add(voice);
        return voice;
    }

    public void Dispose()
    {
        foreach (IAudioVoice voice in voices)
        {
            voice.Dispose();
        }

        voices.Clear();

        if (pin.IsAllocated)
        {
            pin.Free();
        }
    }

    //a tempo change needs its own stream to happen in, which is per voice, so nothing is shared then
    private bool Shareable() => !device.TimeStretch;
}
