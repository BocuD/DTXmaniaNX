using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;

namespace DTXMania;

public class ResultParameterPanel : UIGroup
{
    public ResultParameterPanel()
    {
        name = "ResultParameterPanel";
        scale.X = 0.96f;
        
        AddRow("Perfect", "0000");
        AddRow("Great", "0000");
        AddRow("Good", "0000");
        AddRow("Ok", "0000");
        AddRow("Miss", "0000");
        AddRow("Max Combo", "0000");
        AddRow("Score", "0000");
    }
    
    private float yPos;
    public void AddRow(string name, string label = "")
    {
        var text = AddChild(new UIText(name));
        text.name = name + "Label";
        text.outlineWidth = 0;
        text.fontSize = 20;
        text.fontSource = FontSource.System;
        text.font = "Futura PT Medium.otf";
        text.position.Y = yPos;

        var numText = AddChild(new UIText(label));
        numText.name = name + "Value";
        numText.outlineWidth = 0;
        numText.fontSize = 24;
        numText.fontSource = FontSource.System;
        numText.font = "Futura PT Medium.otf";
        numText.position = new Vector3(107, yPos - 5, 0);
        numText.fillColor = new Color4(0.31f, 0.31f, 0.31f);

        yPos += 24.0f;
    }
}