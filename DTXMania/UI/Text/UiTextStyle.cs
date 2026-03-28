namespace DTXMania.UI.Text;

[Flags]
public enum UiTextStyle
{
    Regular = 0,
    Italic = 1 << 0,
    Bold = 1 << 1,
    Underline = 1 << 2
}
