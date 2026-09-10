using System.IO.Compression;
using System.Text;
using System.Text.Unicode;

namespace DTXMania.SongDb;

public static class ZipEntryNames
{
    //latin-1 maps every byte to the char of the same value and back so no data should be lost
    private static readonly Encoding ByteForByte = Encoding.Latin1;

    private static readonly Encoding StrictShiftJis = Encoding.GetEncoding(
        ChartTextEncoding.ShiftJis.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

    public static ZipArchive Open(Stream stream)
        => new(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: ByteForByte);

    //try to parse filenames in encodings that make sense if the UTF8 flag is not set
    public static string Decode(string name)
    {
        //a char above latin-1 could only have come from a name thats already decoded because of the UTF8 flag
        foreach (char c in name)
        {
            if (c > 0xFF)
            {
                return name;
            }
        }

        byte[] bytes = ByteForByte.GetBytes(name);

        if (Ascii.IsValid(bytes))
        {
            return name;
        }

        if (Utf8.IsValid(bytes))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        try
        {
            return StrictShiftJis.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return name;
        }
    }
}
