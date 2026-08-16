using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK;
using SharpDX.Multimedia;
using Un4seen.Bass;

namespace DTXMania.Core.Audio;

/// <summary>
/// Turns a file BASS cannot read well into a plain WAV in memory.
/// </summary>
public static class PcmDecoder
{
    /// <summary>
    /// The file as a WAV image, or null if BASS should read it itself. Only .xa, which BASS does not know
    /// at all, and RIFF-chunked Vorbis, which it reads with a seek that is off by about 10ms.
    /// </summary>
    public static byte[]? DecodeIfNeeded(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".xa")
        {
            return Decode(path);
        }

        return extension == ".wav" && IsRiffVorbis(path) ? Decode(path) : null;
    }

    /// <summary>Decodes the whole file to 16-bit PCM with a WAV header on the front.</summary>
    public static byte[] Decode(string path)
    {
        SoundDecoder decoder = Path.GetExtension(path).Equals(".xa", StringComparison.OrdinalIgnoreCase)
            ? new Cxa()
            : new Cmp3ogg();

        if (decoder.Open(path) < 0)
        {
            throw new InvalidDataException($"'{Path.GetFileName(path)}' could not be opened for decoding.");
        }

        try
        {
            CWin32.WAVEFORMATEX format = decoder.wfx;

            if (format.wFormatTag == 0)
            {
                throw new InvalidDataException($"'{Path.GetFileName(path)}' gave no wave format.");
            }

            long size = decoder.nTotalPCMSize;

            if (size == 0)
            {
                throw new InvalidDataException($"'{Path.GetFileName(path)}' decoded to nothing.");
            }

            //rounded up to a whole 16-bit sample, so the header's data size describes an exact number
            size += size % 2;

            byte[] wav = new byte[HeaderBytes + size];

            if (decoder.Decode(ref wav, HeaderBytes) < 0)
            {
                throw new InvalidDataException($"'{Path.GetFileName(path)}' failed to decode.");
            }

            WriteHeader(wav, WaveFormatPcm, format.nChannels, format.nSamplesPerSec,
                format.wBitsPerSample, (int)size);

            return wav;
        }
        finally
        {
            decoder.Close();
        }
    }

    /// <summary>
    /// The whole file decoded to 32-bit float PCM, or null if it would come to more than
    /// <paramref name="limitBytes"/>. Every voice of a clip reads the same memory rather than decoding
    /// the file again.
    /// </summary>
    public static byte[]? DecodeToFloat(string path, long limitBytes)
    {
        int decoder = Bass.BASS_StreamCreateFile(path, 0, 0,
            BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);

        if (decoder == 0)
        {
            return null;
        }

        try
        {
            long length = Bass.BASS_ChannelGetLength(decoder);

            if (length <= 0 || length > limitBytes)
            {
                return null;
            }

            BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(decoder);
            byte[] wav = new byte[HeaderBytes + length];

            if (!Read(decoder, wav, length))
            {
                return null;
            }

            WriteHeader(wav, WaveFormatFloat, (ushort)info.chans, (uint)info.freq, 32, (int)length);
            return wav;
        }
        finally
        {
            Bass.BASS_StreamFree(decoder);
        }
    }

    private static bool Read(int decoder, byte[] wav, long length)
    {
        GCHandle pin = GCHandle.Alloc(wav, GCHandleType.Pinned);

        try
        {
            long written = 0;

            while (written < length)
            {
                int wanted = (int)Math.Min(64 * 1024, length - written);
                int read = Bass.BASS_ChannelGetData(decoder,
                    IntPtr.Add(pin.AddrOfPinnedObject(), (int)(HeaderBytes + written)), wanted);

                //-1 is an error, 0 is the end before the length said it would come
                if (read <= 0)
                {
                    return read == 0;
                }

                written += read;
            }

            return true;
        }
        finally
        {
            pin.Free();
        }
    }

    private const int HeaderBytes = 44;

    private const ushort WaveFormatPcm = 1;
    private const ushort WaveFormatFloat = 3;

    private static bool IsRiffVorbis(string path)
    {
        try
        {
            using SoundStream stream = new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            return stream.Format.Encoding is WaveFormatEncoding.OggVorbisMode2Plus
                or WaveFormatEncoding.OggVorbisMode3Plus;
        }
        catch (Exception e)
        {
            //RIFF-chunked mp3 lands here, as does anything SharpDX will not open; BASS gets it either way
            Trace.TraceWarning($"'{Path.GetFileName(path)}' could not be inspected, so BASS will read it "
                               + $"as it is: {e.Message}");
            return false;
        }
    }

    private static void WriteHeader(byte[] wav, ushort tag, ushort channels, uint rate, ushort bits,
        int dataBytes)
    {
        ushort blockAlign = (ushort)(channels * bits / 8);

        using MemoryStream stream = new(wav, 0, HeaderBytes);
        using BinaryWriter writer = new(stream);

        writer.Write("RIFF"u8);
        writer.Write((uint)(dataBytes + HeaderBytes - 8));
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16u);
        writer.Write(tag);
        writer.Write(channels);
        writer.Write(rate);
        writer.Write(rate * blockAlign);
        writer.Write(blockAlign);
        writer.Write(bits);
        writer.Write("data"u8);
        writer.Write((uint)dataBytes);
    }
}
