using System.Diagnostics;
using SharpDX.DirectSound;
using SharpDX.Multimedia;

namespace DTXMania.Core.Audio;

/// <summary>
/// A file decoded once into a secondary buffer. Extra voices duplicate that buffer, which shares the
/// audio data and costs only a playback position.
/// </summary>
internal sealed class DirectSoundClip : IAudioClip
{
    private readonly DirectSoundAudioDevice device;
    private readonly List<DirectSoundVoice> voices = [];

    private SecondarySoundBuffer? first;
    private WaveFormat format = null!;

    public string VoiceKind { get; private set; } = "buffer";

    public long LengthMs { get; private set; }

    public DirectSoundClip(DirectSoundAudioDevice device, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        this.device = device;

        byte[] wav = Read(path);
        (int offset, int length) = Parse(wav, ref format);

        first = new SecondarySoundBuffer(device.Output, new SoundBufferDescription
        {
            Format = format,
            Flags = DirectSoundAudioDevice.BufferFlags,
            BufferBytes = length
        });

        first.Write(wav, offset, length, 0, LockFlags.None);

        LengthMs = (long)(length / (format.AverageBytesPerSecond * 0.001));
        Interlocked.Increment(ref device.Clips);
    }

    public IAudioVoice? CreateVoice()
    {
        if (first == null)
        {
            return null;
        }

        //the first caller gets the buffer that was filled; the rest get copies of it
        SoundBuffer buffer = voices.Count == 0 ? first : Duplicate();

        if (voices.Count > 0)
        {
            VoiceKind = "duplicate";
        }

        DirectSoundVoice voice = new(buffer, format);
        voices.Add(voice);
        return voice;
    }

    private SoundBuffer Duplicate() => device.Output.DuplicateSoundBuffer(first);

    public void Dispose()
    {
        foreach (DirectSoundVoice voice in voices)
        {
            voice.Dispose();
        }

        voices.Clear();

        if (first == null)
        {
            return;
        }

        //the first voice disposed it already if one was ever made
        if (!first.IsDisposed)
        {
            first.Dispose();
        }

        first = null;
        Interlocked.Decrement(ref device.Clips);
    }

    /// <summary>The file as a WAV image, decoding it only when it is not already plain PCM.</summary>
    private static byte[] Read(string path)
    {
        if (PcmDecoder.DecodeIfNeeded(path) is { } decoded)
        {
            return decoded;
        }

        //a compressed file always needs decoding; a .wav usually does not, so it is worth looking
        if (Path.GetExtension(path).ToLowerInvariant() is ".wav")
        {
            try
            {
                using SoundStream stream = new(new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite));

                if (stream.Format.Encoding == WaveFormatEncoding.Pcm)
                {
                    return File.ReadAllBytes(path);
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning($"'{Path.GetFileName(path)}' could not be read as a WAV ({e.Message}); "
                                   + "decoding it.");
            }
        }

        return PcmDecoder.Decode(path);
    }

    /// <summary>Finds the format and where the samples start. Chunks can come in any order and there can
    /// be ones we do not care about between them.</summary>
    private static (int Offset, int Length) Parse(byte[] wav, ref WaveFormat format)
    {
        using MemoryStream stream = new(wav);
        using BinaryReader reader = new(stream);

        if (reader.ReadUInt32() != 0x46464952)
        {
            throw new InvalidDataException("Not a RIFF file.");
        }

        reader.ReadInt32();

        if (reader.ReadUInt32() != 0x45564157)
        {
            throw new InvalidDataException("Not a WAVE file.");
        }

        WaveFormat? found = null;
        int offset = -1;
        int length = -1;

        //+8 is the chunk name and its size; fewer bytes than that left is the end
        while (stream.Position + 8 < stream.Length)
        {
            uint chunk = reader.ReadUInt32();

            switch (chunk)
            {
                case 0x20746D66:
                    found = ReadFormat(reader, out long formatBytes);
                    stream.Seek(formatBytes, SeekOrigin.Current);
                    break;

                case 0x61746164:
                    length = reader.ReadInt32();
                    offset = (int)stream.Position;
                    stream.Seek(length, SeekOrigin.Current);
                    break;

                default:
                    stream.Seek(reader.ReadUInt32(), SeekOrigin.Current);
                    break;
            }
        }

        if (found == null || offset < 0)
        {
            throw new InvalidDataException("The file has no fmt or data chunk.");
        }

        format = found;
        return (offset, Math.Min(length, wav.Length - offset));
    }

    private static WaveFormat ReadFormat(BinaryReader reader, out long remaining)
    {
        long size = reader.ReadUInt32();

        WaveFormatEncoding tag = (WaveFormatEncoding)reader.ReadUInt16();
        short channels = reader.ReadInt16();
        int rate = reader.ReadInt32();
        int bytesPerSecond = reader.ReadInt32();
        short blockAlign = reader.ReadInt16();
        short bits = reader.ReadInt16();

        long read = 16;

        WaveFormat format;

        switch (tag)
        {
            case WaveFormatEncoding.Pcm:
                format = WaveFormat.CreateCustomFormat(tag, rate, channels, bytesPerSecond, blockAlign, bits);
                break;

            case WaveFormatEncoding.Extensible:
                WaveFormatExtensible extensible = (WaveFormatExtensible)
                    WaveFormatExtensible.CreateCustomFormat(tag, rate, channels, bytesPerSecond, blockAlign, bits);

                reader.ReadUInt16();
                reader.ReadInt16();
                extensible.ChannelMask = (Speakers)reader.ReadInt32();
                extensible.GuidSubFormat = new Guid(reader.ReadBytes(16));

                read += 24;
                format = extensible;
                break;

            default:
                throw new InvalidDataException($"Unsupported wave format tag {tag}.");
        }

        remaining = size - read;
        return format;
    }
}
