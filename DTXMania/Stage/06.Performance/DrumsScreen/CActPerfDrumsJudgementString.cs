using DTXMania.Core;

namespace DTXMania;

internal class CActPerfDrumsJudgementString : CActPerfCommonJudgementString
{
    public CActPerfDrumsJudgementString()
    {
        bActivated = false;
    }

    protected override int LaneCount => (int)ELane.LBD + 1;

    //Start takes lanes up to 14; only drum lanes draw
    protected override void InitializeLaneSizes()
    {
        stLaneSize = new STLaneSize[15];
    }

    protected override bool TryGetLanePosition(int lane, out int x, out int y)
    {
        if (lane > (int)ELane.LBD)
        {
            x = y = 0;
            return false;
        }

        x = (int)DrumLanes.Centre((ELane)lane);
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