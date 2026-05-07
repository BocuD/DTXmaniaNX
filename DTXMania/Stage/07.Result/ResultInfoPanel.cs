using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania;

public class ResultInfoPanel : UIGroup
{
    public ResultInfoPanel()
    {
        name = "ResultInfo";
        
        var white = BaseTexture.CreateSolidColor(Color4.White);
        
        var levelIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_level.png"))));
        levelIcon.position = new Vector3(64, 21, 0);
        var levelLine = AddChild(new UIImage(white));
        levelLine.position = new Vector3(88, 94, 0);
        levelLine.size = new Vector2(340, 2);
        
        var rateIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_rate.png"))));
        rateIcon.position = new Vector3(32, 77, 0);
        var rateLine  = AddChild(new UIImage(white));
        rateLine.position = new Vector3(60, 168, 0);
        rateLine.size = new Vector2(344, 2);
        
        var skillIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_skill.png"))));
        skillIcon.position = new Vector3(7, 194, 0);
        skillIcon.scale = new Vector3(0.67f, 0.67f, 1.0f);
        
        var skillText = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\label_skill.png"))));
        skillText.position = new Vector3(18, 264, 0);
        skillText.scale = new Vector3(0.67f, 0.67f, 1.0f);
        var skillLine  = AddChild(new UIImage(white));
        skillLine.position = new Vector3(14, 296, 0);
        skillLine.size = new Vector2(340, 2);
    }
}