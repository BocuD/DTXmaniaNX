using System.Globalization;

namespace DTXMania;

/// <summary>
/// Formats a number into a caller's buffer for the panels that draw digits from a sprite sheet.
/// </summary>
internal static class PanelNumber
{
    private static ReadOnlySpan<char> Justify(Span<char> destination, int written, int width)
    {
        if (written >= width)
        {
            return destination[..written];
        }

        destination[..written].CopyTo(destination[(width - written)..]);
        destination[..(width - written)].Fill(' ');
        return destination[..width];
    }

    public static ReadOnlySpan<char> Format(Span<char> destination, int value, int width = 0)
    {
        value.TryFormat(destination, out int written, default, CultureInfo.InvariantCulture);
        return Justify(destination, written, width);
    }

    public static ReadOnlySpan<char> Format(Span<char> destination, int value,
        ReadOnlySpan<char> format, int width = 0)
    {
        value.TryFormat(destination, out int written, format, CultureInfo.InvariantCulture);
        return Justify(destination, written, width);
    }

    public static ReadOnlySpan<char> Format(Span<char> destination, double value,
        ReadOnlySpan<char> format, int width = 0)
    {
        value.TryFormat(destination, out int written, format, CultureInfo.InvariantCulture);
        return Justify(destination, written, width);
    }

    public static ReadOnlySpan<char> Percent(Span<char> destination, double value,
        ReadOnlySpan<char> format, int width)
    {
        ReadOnlySpan<char> number = Format(destination, value, format, width);
        destination[number.Length] = '%';
        return destination[..(number.Length + 1)];
    }
}
