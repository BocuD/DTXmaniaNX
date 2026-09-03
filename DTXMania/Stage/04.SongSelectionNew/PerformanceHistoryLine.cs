using DTXMania.Core;

namespace DTXMania;

public enum EPerformanceOutcome
{
    Unknown,
    Cleared,
    Failed,
    Cancelled
}

public readonly record struct PerformanceHistoryLine
{
    public string Raw { get; init; }

    /// <summary>0 when the line does not say.</summary>
    public int PlayNumber { get; init; }

    /// <summary>Normalised to <c>yyyy/MM/dd</c>. The file writes a two digit year, which is read as
    /// 20xx.</summary>
    public string Date { get; init; }

    public EPerformanceOutcome Outcome { get; init; }
    public EInstrumentPart Instrument { get; init; }

    /// <summary><see cref="CScoreIni.ERANK"/>, or UNKNOWN when the line carries no rank.</summary>
    public int Rank { get; init; }

    //strings because they are only ever displayed
    public string Skill { get; init; }
    public string Speed { get; init; }

    public static PerformanceHistoryLine Empty => new()
    {
        Raw = string.Empty,
        Date = string.Empty,
        Skill = string.Empty,
        Speed = string.Empty,
        Instrument = EInstrumentPart.UNKNOWN,
        Rank = (int)CScoreIni.ERANK.UNKNOWN
    };

    public static PerformanceHistoryLine TryRead(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return Empty;
        }

        PerformanceHistoryLine parsed = Empty with { Raw = line.Trim() };

        try
        {
            return Read(parsed);
        }
        catch (Exception)
        {
            return parsed;
        }
    }

    private static PerformanceHistoryLine Read(PerformanceHistoryLine parsed)
    {
        string rest = parsed.Raw;

        //"12.25/9/1 rest": the count and the date run together, so the first dot splits them
        int dot = rest.IndexOf('.');
        int firstSpace = rest.IndexOf(' ');

        if (dot > 0 && (firstSpace < 0 || dot < firstSpace) && int.TryParse(rest[..dot], out int playNumber))
        {
            parsed = parsed with { PlayNumber = playNumber };
            rest = rest[(dot + 1)..];
        }

        int dateEnd = rest.IndexOf(' ');
        if (dateEnd > 0)
        {
            parsed = parsed with { Date = ReadDate(rest[..dateEnd]) };
            rest = rest[(dateEnd + 1)..];
        }

        parsed = parsed with
        {
            Outcome = ReadOutcome(rest),
            Instrument = ReadInstrument(rest),
            Speed = ReadSpeed(rest)
        };

        //the rank and skill live in the one bracketed group a cleared line ends with
        int open = rest.IndexOf('(');
        int colon = rest.IndexOf(':');
        int close = rest.LastIndexOf(')');

        if (open < 0 || colon < open || close < colon)
        {
            return parsed;
        }

        string rank = rest[(open + 1)..colon].Trim();
        if (Enum.TryParse(rank, ignoreCase: true, out CScoreIni.ERANK parsedRank))
        {
            parsed = parsed with { Rank = (int)parsedRank };
        }

        //everything after the rank is the skill, up to where the speed starts
        string tail = rest[(colon + 1)..close].Trim();
        int speedAt = tail.IndexOf('x');
        if (speedAt >= 0)
        {
            tail = tail[..speedAt];
        }

        return parsed with { Skill = tail.Replace("Speed", string.Empty).Trim() };
    }

    //y/m/d in any width, padded out so the column lines up. Anything else is left alone
    private static string ReadDate(string date)
    {
        string[] parts = date.Split('/');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int year)
            || !int.TryParse(parts[1], out int month)
            || !int.TryParse(parts[2], out int day))
        {
            return date;
        }

        if (year < 100)
        {
            year += 2000;
        }

        return $"{year:D4}/{month:D2}/{day:D2}";
    }

    private static EPerformanceOutcome ReadOutcome(string body)
    {
        if (body.StartsWith("Cleared", StringComparison.OrdinalIgnoreCase))
        {
            return EPerformanceOutcome.Cleared;
        }

        if (body.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            return EPerformanceOutcome.Failed;
        }

        if (body.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
            || body.Contains("canceled", StringComparison.OrdinalIgnoreCase))
        {
            return EPerformanceOutcome.Cancelled;
        }

        return EPerformanceOutcome.Unknown;
    }

    //G+B first: it says neither "Guitar" nor "Bass", while a fork's "Guitar/Bass" says both
    private static EInstrumentPart ReadInstrument(string body)
    {
        if (body.Contains("G+B", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Guitar/Bass", StringComparison.OrdinalIgnoreCase))
        {
            return EInstrumentPart.GUITAR;
        }

        if (body.Contains("Drums", StringComparison.OrdinalIgnoreCase))
        {
            return EInstrumentPart.DRUMS;
        }

        if (body.Contains("Guitar", StringComparison.OrdinalIgnoreCase))
        {
            return EInstrumentPart.GUITAR;
        }

        return body.Contains("Bass", StringComparison.OrdinalIgnoreCase)
            ? EInstrumentPart.BASS
            : EInstrumentPart.UNKNOWN;
    }

    //written as " x1.50" or " Speed x1.50", and left out entirely at normal speed
    private static string ReadSpeed(string body)
    {
        int at = body.IndexOf('x');
        if (at < 0 || at + 1 >= body.Length || !char.IsDigit(body[at + 1]))
        {
            return string.Empty;
        }

        int end = at + 1;
        while (end < body.Length && (char.IsDigit(body[end]) || body[end] == '.'))
        {
            end++;
        }

        return body[(at + 1)..end];
    }
}
