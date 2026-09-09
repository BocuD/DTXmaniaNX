using System.Text;
using System.Text.Unicode;

namespace DTXMania;

/// <summary>
/// Picks the encoding a chart or song definition file was written in. These are historically Shift-JIS,
/// but editors write UTF-8 as well, and nothing in the format says which.
/// </summary>
public static class ChartTextEncoding
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] Utf16LeBom = [0xFF, 0xFE];
    private static readonly byte[] Utf16BeBom = [0xFE, 0xFF];

    static ChartTextEncoding()
    {
        //Program.cs does this too, for anything that reads a chart without starting the game
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static Encoding ShiftJis => Encoding.GetEncoding("shift-jis");

    /// <summary>
    /// What <paramref name="bytes"/> are written in. Shift-JIS above ASCII is almost never a valid UTF-8
    /// sequence, so decoding cleanly as UTF-8 is what tells the two apart; anything that does not is
    /// Shift-JIS. Bytes that are entirely ASCII read the same either way and are reported as Shift-JIS.
    /// </summary>
    public static Encoding Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Utf8Bom))
        {
            return Encoding.UTF8;
        }

        if (bytes.StartsWith(Utf16LeBom))
        {
            return Encoding.Unicode;
        }

        if (bytes.StartsWith(Utf16BeBom))
        {
            return Encoding.BigEndianUnicode;
        }

        if (IsAscii(bytes))
        {
            return ShiftJis;
        }

        return Utf8.IsValid(bytes) ? Encoding.UTF8 : ShiftJis;
    }

    public static Encoding Detect(string path) => Detect(File.ReadAllBytes(path));

    /// <summary>Opens a file, reading it as whatever it turns out to be written in.</summary>
    public static StreamReader OpenText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return new StreamReader(new MemoryStream(bytes), Detect(bytes));
    }

    public static string[] ReadAllLines(string path)
    {
        List<string> lines = [];
        using StreamReader reader = OpenText(path);

        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static bool IsAscii(ReadOnlySpan<byte> bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] >= 0x80)
            {
                return false;
            }
        }

        return true;
    }
}
