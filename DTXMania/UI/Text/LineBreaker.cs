using DTXMania.SongDb;
using Kawazu;

namespace DTXMania.UI.Text;

public static class LineBreaker
{
    /// <summary>Greedily fills lines up to <paramref name="budget"/>, measuring with <paramref name="measure"/>.</summary>
    public static List<string> Wrap(string line, float budget, Func<string, float> measure)
    {
        List<string> wrapped = [];

        if (line.Length == 0 || measure(line) <= budget)
        {
            wrapped.Add(line);
            return wrapped;
        }

        int[] opportunities = Opportunities(line);
        int lineStart = 0;
        int lastFit = -1;

        for (int i = 0; i <= opportunities.Length; i++)
        {
            //the end of the text is the last place a line can run to
            int offset = i < opportunities.Length ? opportunities[i] : line.Length;
            if (offset <= lineStart)
            {
                continue;
            }

            if (measure(line[lineStart..offset].TrimEnd()) <= budget)
            {
                lastFit = offset;
                continue;
            }

            if (lastFit > lineStart)
            {
                wrapped.Add(line[lineStart..lastFit].TrimEnd());
                lineStart = lastFit;
            }
            else
            {
                //nothing breakable fits, so the run is chopped mid-word rather than left to overflow
                int taken = LongestFit(line[lineStart..offset], budget, measure);
                wrapped.Add(line.Substring(lineStart, taken));
                lineStart += taken;
            }

            lastFit = -1;
            i--; //this offset still has to be placed on the line that just started
        }

        if (lineStart < line.Length)
        {
            wrapped.Add(line[lineStart..].TrimEnd());
        }

        return wrapped;
    }

    //how many characters of `run` fit, at least one so a caller can always make progress
    private static int LongestFit(string run, float budget, Func<string, float> measure)
    {
        int low = 1;
        int high = run.Length;

        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (measure(run[..mid]) <= budget)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    //offsets are the start of the line that follows the break
    private static readonly Dictionary<string, int[]> Cache = new(StringComparer.Ordinal);
    private static readonly object CacheSync = new();
    private const int MaxCachedLines = 512;

    //a line may not start with these
    private const string NoLineStart = "。、，．・：；！？）〕］｝」』〉》々ー～ぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮヵヶ,.!?:;)]}\"'";

    //a line may not end with these
    private const string NoLineEnd = "（〔［｛「『〈《([{\"'";

    public static int[] Opportunities(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return [];
        }

        lock (CacheSync)
        {
            if (Cache.TryGetValue(line, out int[]? cached))
            {
                return cached;
            }
        }

        int[] built = Build(line);

        lock (CacheSync)
        {
            //text flowing through a UIText is unbounded, so the cache is capped rather than grown
            if (Cache.Count >= MaxCachedLines)
            {
                Cache.Clear();
            }

            Cache[line] = built;
        }

        return built;
    }

    private static int[] Build(string line)
    {
        SortedSet<int> offsets = [];

        AddWhitespaceBreaks(line, offsets);

        if (!Utilities.HasJapanese(line) || !AddMorphemeBreaks(line, offsets))
        {
            AddCjkBreaks(line, offsets);
        }

        offsets.RemoveWhere(offset => IsForbidden(line, offset));
        return offsets.ToArray();
    }

    private static void AddWhitespaceBreaks(string line, SortedSet<int> offsets)
    {
        for (int i = 1; i < line.Length; i++)
        {
            //the break goes after a run of spaces, so the next line does not start with one
            if (char.IsWhiteSpace(line[i - 1]) && !char.IsWhiteSpace(line[i]))
            {
                offsets.Add(i);
            }
        }
    }

    private static bool AddMorphemeBreaks(string line, SortedSet<int> offsets)
    {
        List<string> divisions = TextConversionCache.Segment(line);

        //one division is the whole line, which says nothing about where it may break
        if (divisions.Count < 2)
        {
            return false;
        }

        int offset = 0;
        foreach (string division in divisions)
        {
            offset += division.Length;

            if (offset > 0 && offset < line.Length)
            {
                offsets.Add(offset);
            }
        }

        return true;
    }

    private static void AddCjkBreaks(string line, SortedSet<int> offsets)
    {
        for (int i = 1; i < line.Length; i++)
        {
            if (IsCjk(line[i - 1]) && IsCjk(line[i]))
            {
                offsets.Add(i);
            }
        }
    }

    private static bool IsForbidden(string line, int offset)
    {
        if (offset <= 0 || offset >= line.Length)
        {
            return true;
        }

        return NoLineStart.Contains(line[offset]) || NoLineEnd.Contains(line[offset - 1]);
    }

    private static bool IsCjk(char c)
    {
        return c is >= '぀' and <= 'ヿ'    //hiragana, katakana
            or >= '㐀' and <= '䶿'         //CJK extension A
            or >= '一' and <= '鿿'         //CJK unified
            or >= '豈' and <= '﫿'         //compatibility ideographs
            or >= '가' and <= '힯';        //hangul syllables
    }
}
