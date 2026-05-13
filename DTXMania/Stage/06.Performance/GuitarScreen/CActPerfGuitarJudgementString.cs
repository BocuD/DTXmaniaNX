using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class CActPerfGuitarJudgementString : CActPerfCommonJudgementString
{
    public CActPerfGuitarJudgementString()
    {
        bActivated = false;
    }

    protected override int LaneCount => 15;

    protected override void InitializeLaneSizes()
    {
        stLaneSize = new STLaneSize[15];
        int[,] sizeXW = new[,]
        {
            { 30, 36 }, { 71, 30 }, { 135, 30 }, { 202, 30 }, { 167, 30 }, { 237, 30 }, { 269, 30 }, { 333, 36 },
            { 103, 30 }, { 301, 30 }, { 103, 30 }, { 0, 0 }, { 0, 0 }, { 26, 111 }, { 480, 111 }
        };

        for (int i = 0; i < 15; i++)
        {
            stLaneSize[i] = new STLaneSize
            {
                x = sizeXW[i, 0],
                w = sizeXW[i, 1]
            };
        }
    }

    protected override bool TryGetLanePosition(int lane, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (lane == 14)
        {
            if (CDTXMania.ConfigIni.JudgementStringPosition.Bass == EType.D)
            {
                return false;
            }

            x = (CDTXMania.ConfigIni.JudgementStringPosition.Bass == EType.B) ? 770 : 1060;
            y = CDTXMania.ConfigIni.JudgementStringPosition.Bass == EType.C
                ? (CDTXMania.ConfigIni.bReverse.Bass ? 650 : 80)
                : (CDTXMania.ConfigIni.bReverse.Bass ? 450 : 300);
            return true;
        }

        if (lane == 13)
        {
            if (CDTXMania.ConfigIni.JudgementStringPosition.Guitar == EType.D)
            {
                return false;
            }

            x = (CDTXMania.ConfigIni.JudgementStringPosition.Guitar == EType.B) ? 420 : 180;
            y = CDTXMania.ConfigIni.JudgementStringPosition.Guitar == EType.C
                ? (CDTXMania.ConfigIni.bReverse.Guitar ? 650 : 80)
                : (CDTXMania.ConfigIni.bReverse.Guitar ? 450 : 300);
        }

        return true;
    }

    protected override BaseTexture GetJudgeTexture()
    {
        return CDTXMania.stagePerfGuitarScreen.tx判定画像anime;
    }

    protected override bool ShouldDrawJudgementString()
    {
        return CDTXMania.ConfigIni.bDisplayJudge.Guitar || CDTXMania.ConfigIni.bDisplayJudge.Bass;
    }
}