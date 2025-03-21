using DTXUIRenderer;

namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public string name { get; set; }
    public string author { get; set; }

    public Dictionary<CStage.EStage, UIGroup> stageRootNodes;
}