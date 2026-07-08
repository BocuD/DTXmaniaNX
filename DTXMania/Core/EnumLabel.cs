using System.Reflection;

namespace DTXMania.Core;

[AttributeUsage(AttributeTargets.Field)]
public sealed class EnumLabelAttribute : Attribute
{
    public string English { get; }
    public string Japanese { get; }

    public EnumLabelAttribute(string english, string? japanese = null)
    {
        English = english;
        Japanese = japanese ?? english;
    }
}

/// <summary>Resolves the display text for an enum member (see <see cref="EnumLabelAttribute"/>).</summary>
public static class EnumLabels
{
    private static readonly Dictionary<Enum, (string en, string jp)> cache = new();

    /// <summary>The localized display label for <paramref name="value"/> (attribute text, else its name).</summary>
    public static string Get<T>(T value) where T : struct, Enum => Get((Enum)value);

    public static string Get(Enum value)
    {
        if (!cache.TryGetValue(value, out (string en, string jp) labels))
        {
            EnumLabelAttribute? attr = value.GetType().GetField(value.ToString())?.GetCustomAttribute<EnumLabelAttribute>();
            labels = attr != null ? (attr.English, attr.Japanese) : (value.ToString(), value.ToString());
            cache[value] = labels;
        }

        return CDTXMania.isJapanese ? labels.jp : labels.en;
    }
}
