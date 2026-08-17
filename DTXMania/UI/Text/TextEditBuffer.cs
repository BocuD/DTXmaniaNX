using System.Globalization;

namespace DTXMania.UI.Text;

/// <summary>
/// What a text field holds while it is being edited: the string, where the caret is, and what is
/// selected. Indices are into the string and always land on a text element boundary, so a surrogate pair
/// or a combining mark is never cut in half.
/// </summary>
public sealed class TextEditBuffer
{
    public string text { get; private set; } = string.Empty;

    /// <summary>Where typing lands. The selection is everything between here and <see cref="anchor"/>.</summary>
    public int caret { get; private set; }

    public int anchor { get; private set; }

    public int maxLength = 128;

    public bool HasSelection => caret != anchor;
    public int SelectionStart => Math.Min(caret, anchor);
    public int SelectionEnd => Math.Max(caret, anchor);
    public string SelectedText => text[SelectionStart..SelectionEnd];

    public void Set(string value, bool selectAll)
    {
        text = Sanitize(value ?? string.Empty);

        if (text.Length > maxLength)
        {
            text = text[..BoundaryAtOrBefore(text, maxLength)];
        }

        caret = text.Length;
        anchor = selectAll ? 0 : caret;
    }

    public void Insert(string value)
    {
        string insert = Sanitize(value);
        if (insert.Length == 0)
        {
            return;
        }

        DeleteSelection();

        int room = maxLength - text.Length;
        if (room <= 0)
        {
            return;
        }

        if (insert.Length > room)
        {
            insert = insert[..BoundaryAtOrBefore(insert, room)];
        }

        text = text.Insert(caret, insert);
        caret += insert.Length;
        anchor = caret;
    }

    public void Backspace(bool wholeWord)
    {
        if (DeleteSelection())
        {
            return;
        }

        int from = wholeWord ? PreviousWord(caret) : PreviousBoundary(caret);
        Remove(from, caret);
    }

    public void Delete(bool wholeWord)
    {
        if (DeleteSelection())
        {
            return;
        }

        int to = wholeWord ? NextWord(caret) : NextBoundary(caret);
        Remove(caret, to);
    }

    public void MoveLeft(bool extend, bool wholeWord)
    {
        //a selection collapses to its near edge rather than moving on, which is what every text field does
        if (HasSelection && !extend && !wholeWord)
        {
            MoveTo(SelectionStart, false);
            return;
        }

        MoveTo(wholeWord ? PreviousWord(caret) : PreviousBoundary(caret), extend);
    }

    public void MoveRight(bool extend, bool wholeWord)
    {
        if (HasSelection && !extend && !wholeWord)
        {
            MoveTo(SelectionEnd, false);
            return;
        }

        MoveTo(wholeWord ? NextWord(caret) : NextBoundary(caret), extend);
    }

    public void MoveHome(bool extend) => MoveTo(0, extend);

    public void MoveEnd(bool extend) => MoveTo(text.Length, extend);

    public void MoveTo(int index, bool extend)
    {
        caret = BoundaryAtOrBefore(text, Math.Clamp(index, 0, text.Length));

        if (!extend)
        {
            anchor = caret;
        }
    }

    public void SelectAll()
    {
        anchor = 0;
        caret = text.Length;
    }

    /// <summary>Selects the run around an index, which is what a double click asks for.</summary>
    public void SelectWordAt(int index)
    {
        index = Math.Clamp(index, 0, text.Length);

        //a click at the far end of a word selects that word rather than the space after it
        int from = index > 0 && (index == text.Length || char.IsWhiteSpace(text[index])) ? index - 1 : index;
        int to = from;

        while (from > 0 && !char.IsWhiteSpace(text[from - 1]))
        {
            from--;
        }

        while (to < text.Length && !char.IsWhiteSpace(text[to]))
        {
            to++;
        }

        anchor = from;
        caret = to;
    }

    /// <summary>Removes the selection and returns what it held, for the clipboard.</summary>
    public string CutSelection()
    {
        string selected = SelectedText;
        DeleteSelection();
        return selected;
    }

    private bool DeleteSelection()
    {
        if (!HasSelection)
        {
            return false;
        }

        Remove(SelectionStart, SelectionEnd);
        return true;
    }

    private void Remove(int from, int to)
    {
        if (to <= from)
        {
            return;
        }

        text = text.Remove(from, to - from);
        caret = from;
        anchor = from;
    }

    //a field is one line, and a control character in the middle of one is never what was meant
    private static string Sanitize(string value)
        => value.Any(char.IsControl) ? new string(value.Where(c => !char.IsControl(c)).ToArray()) : value;

    private int NextBoundary(int index)
        => index >= text.Length ? text.Length : index + StringInfo.GetNextTextElementLength(text.AsSpan(index));

    private int PreviousBoundary(int index)
    {
        int boundary = 0;

        while (boundary < index)
        {
            int next = boundary + StringInfo.GetNextTextElementLength(text.AsSpan(boundary));
            if (next >= index)
            {
                return boundary;
            }

            boundary = next;
        }

        return boundary;
    }

    private static int BoundaryAtOrBefore(string value, int index)
    {
        int boundary = 0;

        while (boundary < index)
        {
            int next = boundary + StringInfo.GetNextTextElementLength(value.AsSpan(boundary));
            if (next > index)
            {
                return boundary;
            }

            boundary = next;
        }

        return boundary;
    }

    //word motion is by runs of whitespace and runs of everything else, which is all a language without
    //spaces gives us to go on: in Japanese one press crosses the whole run
    private int NextWord(int index)
    {
        int position = index;

        while (position < text.Length && !char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    private int PreviousWord(int index)
    {
        int position = index;

        while (position > 0 && char.IsWhiteSpace(text[position - 1]))
        {
            position--;
        }

        while (position > 0 && !char.IsWhiteSpace(text[position - 1]))
        {
            position--;
        }

        return position;
    }
}
