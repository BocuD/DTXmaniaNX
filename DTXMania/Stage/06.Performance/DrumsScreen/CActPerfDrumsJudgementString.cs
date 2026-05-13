using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class CActPerfDrumsJudgementString : CActPerfCommonJudgementString
{
    public CActPerfDrumsJudgementString()
    {
        bActivated = false;
    }

    protected override int LaneCount => 12;

    protected override void InitializeLaneSizes()
    {
        stLaneSize = new STLaneSize[15];
        int[,] sizeXW = new[,]
        {
            { 290, 80 }, { 367, 46 }, { 470, 54 }, { 582, 60 }, { 528, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
            { 419, 46 }, { 815, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
        };
        int[,] sizeXW_B = new[,]
        {
            { 290, 80 }, { 367, 46 }, { 419, 54 }, { 534, 60 }, { 590, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
            { 478, 46 }, { 815, 64 }, { 815, 80 }, { 507, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
        };
        int[,] sizeXW_C = new[,]
        {
            { 290, 80 }, { 367, 46 }, { 470, 54 }, { 534, 60 }, { 590, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
            { 419, 46 }, { 815, 64 }, { 815, 80 }, { 507, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
        };
        int[,] sizeXW_D = new[,]
        {
            { 290, 80 }, { 367, 46 }, { 419, 54 }, { 582, 60 }, { 476, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
            { 528, 46 }, { 815, 64 }, { 815, 80 }, { 507, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
        };

        for (int i = 0; i < 15; i++)
        {
            stLaneSize[i] = new STLaneSize();
            if (!CDTXMania.ConfigIni.bDrumsEnabled)
            {
                continue;
            }

            switch (CDTXMania.ConfigIni.eLaneType.Drums)
            {
                case EType.A:
                    stLaneSize[i].x = sizeXW[i, 0];
                    stLaneSize[i].w = sizeXW[i, 1];
                    break;
                case EType.B:
                    stLaneSize[i].x = sizeXW_B[i, 0];
                    stLaneSize[i].w = sizeXW_B[i, 1];
                    break;
                case EType.C:
                    stLaneSize[i].x = sizeXW_C[i, 0];
                    stLaneSize[i].w = sizeXW_C[i, 1];
                    break;
                case EType.D:
                    stLaneSize[i].x = sizeXW_D[i, 0];
                    stLaneSize[i].w = sizeXW_D[i, 1];
                    break;
            }

            if (i == 7 && CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
            {
                stLaneSize[i].x = sizeXW[9, 0] - 24;
            }

            if (i == 9 && CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
            {
                stLaneSize[i].x = sizeXW[7, 0] - 18;
            }
        }
    }

    protected override bool TryGetLanePosition(int lane, out int x, out int y)
    {
        x = stLaneSize[lane].x;
        if (CDTXMania.ConfigIni.JudgementStringPosition.Drums == EType.A)
        {
            y = CDTXMania.ConfigIni.bReverse.Drums ? 348 - (n文字の縦表示位置[lane] * 0x20) : 348 + n文字の縦表示位置[lane] * 0x20;
        }
        else
        {
            y = CDTXMania.ConfigIni.bReverse.Drums ? 80 + n文字の縦表示位置[lane] * 0x20 : 583 + n文字の縦表示位置[lane] * 0x20;
        }

        return true;
    }

    protected override BaseTexture GetJudgeTexture()
    {
        return CDTXMania.stagePerfDrumsScreen.tx判定画像anime;
    }

    protected override bool ShouldDrawJudgementString()
    {
        return CDTXMania.ConfigIni.bDisplayJudge.Drums;
    }

    #region [ private ]

    private readonly int[] n文字の縦表示位置 = [-1, 1, 1, 2, 0, 0, 1, -1, 2, 1, 2, -1, -1, 0, 0];

    #endregion
}