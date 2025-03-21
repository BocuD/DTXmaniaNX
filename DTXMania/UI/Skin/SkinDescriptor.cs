namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public required string name { get; set; }
    public required string author { get; set; }

    public Dictionary<CStage.EStage, string> stageSkins { get; set; } = new();
}