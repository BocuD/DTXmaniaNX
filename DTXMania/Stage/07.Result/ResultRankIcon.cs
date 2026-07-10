using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

public class ResultRankIcon : UIGroup
{
    public ResultRankIcon(int instrument)
    {
        name = $"ResultRankIcon";
        anchor = new Vector2(0.5f, 0.5f);
        size = new Vector2(420, 510);
        
        var stageResult = CDTXMania.StageManager.stageResult;
        
        bool bAllAuto = instrument switch
        {
            0 => CDTXMania.ConfigIni.bAllDrumsAreAutoPlay,
            1 => CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay,
            2 => CDTXMania.ConfigIni.bAllBassAreAutoPlay,
            _ => false
        };
        
        BaseTexture txRankIcon = null;
        BaseTexture txRankBg = null;
        bool isSS = false;

        for (int j = 0; j < 3; j++)
        {
            switch (stageResult.nRankValue[instrument])
            {
                case 0:
                    txRankIcon = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_icon_s.png"));
                    txRankBg = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_bg_ss.png"));
                    isSS = true;
                    break;

                case 1:
                    txRankIcon = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_icon_s.png"));
                    txRankBg = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_bg_s.png"));
                    break;

                case 2:
                    txRankIcon = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_icon_a.png"));
                    txRankBg = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_bg_a.png"));
                    break;

                case 3:
                    txRankIcon = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_icon_b.png"));
                    txRankBg = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_bg_b.png"));
                    break;

                case 4:
                case 5:
                case 6:
                case 99:
                    txRankIcon = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_icon_c.png"));
                    txRankBg = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_bg_c.png"));

                    if (bAllAuto)
                    {
                        txRankIcon = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_icon_s.png"));
                        txRankBg = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\rank_bg_ss.png"));
                        isSS = true;
                    }

                    break;

                default:
                    txRankIcon = null;
                    break;
            }
        }
        
        if (txRankIcon != null)
        {
            var icon = AddChild(new UIImage(txRankIcon));
            icon.name = "Icon";
            icon.renderOrder = 0;
            icon.position = new Vector3(132, 150, 0);

            if (isSS)
            {
                icon.position.X = 60;
                
                var icon2 = AddChild(new UIImage(txRankIcon));
                icon2.name = "Icon2";
                icon2.renderOrder = 1;
                icon2.position = new Vector3(205, 150, 0);
            }
        }

        if (txRankBg != null)
        {
            var bg = AddChild(new UIImage(txRankBg));
            bg.name = "Bg";
            bg.renderOrder = -1;
            bg.anchor = new Vector2(0.5f, 0.5f);
            bg.position = new Vector3(210, 255, 0);
        }
        
        BaseTexture rankBadge = null;
        if (stageResult.stPerformanceEntry[instrument].nPerfectCount == stageResult.stPerformanceEntry[instrument].nTotalChipsCount)
        {
            var badge = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\excellent.png"))));
            badge.name = "Badge";
            badge.renderOrder = 1;
            badge.anchor = new Vector2(0.5f, 0);
            badge.scale = new Vector3(0.66f, 0.66f, 1.0f);
            badge.position = new Vector3(210, 350, 0);
        }
        else if (stageResult.stPerformanceEntry[instrument].bIsFullCombo)
        {
            var badge1 = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\fullcombo_0.png"))));
            badge1.name = "Badge";
            badge1.renderOrder = 1;
            badge1.scale = new Vector3(0.66f, 0.66f, 1.0f);
            badge1.position = new Vector3(55, 350, 0);

            var badge2 = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\fullcombo_1.png"))));
            badge2.name = "Badge2";
            badge2.renderOrder = 1;
            badge2.scale = new Vector3(0.66f, 0.66f, 1.0f);
            badge2.position = new Vector3(180, 320, 0);
        }
        else
        {
            var badge1 = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\clear_0.png"))));
            badge1.name = "Badge";
            badge1.renderOrder = 1;
            badge1.scale = new Vector3(0.66f, 0.66f, 1.0f);
            badge1.position = new Vector3(210, 364, 0);
            badge1.anchor = new Vector2(0.5f, 0);

            var badge2 = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\Rank\clear_1.png"))));
            badge2.name = "Badge2";
            badge2.renderOrder = 1;
            badge2.scale = new Vector3(0.66f, 0.66f, 1.0f);
            badge2.position = new Vector3(210, 420, 0);
            badge2.anchor = new Vector2(0.5f, 0);
        }
    }
}