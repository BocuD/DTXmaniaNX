namespace DTXMania.UI.Inspector;

public class AddChildMenuAttribute : Attribute
{
    public string? Path { get; }

    public AddChildMenuAttribute()
    {
    }

    public AddChildMenuAttribute(string path)
    {
        Path = path;
    }
}