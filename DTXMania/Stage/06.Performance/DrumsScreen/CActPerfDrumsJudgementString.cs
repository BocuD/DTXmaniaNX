using DTXMania.Core;

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
        x = stLaneSize[lane].x + (stLaneSize[lane].w / 2);
        if (CDTXMania.ConfigIni.JudgementStringPosition.Drums == EType.A)
        {
            y = CDTXMania.ConfigIni.bReverse.Drums ? 348 - (verticalCharacterOffsets[lane] * 0x20) : 348 + verticalCharacterOffsets[lane] * 0x20;
        }
        else
        {
            y = CDTXMania.ConfigIni.bReverse.Drums
                ? 80 + verticalCharacterOffsets[lane] * 0x20
                : 583 + verticalCharacterOffsets[lane] * 0x20;
        }

        return CDTXMania.ConfigIni.JudgementStringPosition.Drums != EType.C;
    }

    protected override bool ShouldDrawJudgementString()
    {
        return CDTXMania.ConfigIni.bDisplayJudge.Drums && CDTXMania.ConfigIni.JudgementStringPosition.Drums != EType.D;
    }

    #region [ private ]

    private readonly int[] verticalCharacterOffsets = [-1, 1, 1, 2, 0, 0, 1, -1, 2, 1, 2, -1, -1, 0, 0];

    #endregion
}