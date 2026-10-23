using DTXMania.Core;

namespace DTXMania;

internal static class ChipJudge
{
    //lag is the hit time minus the chip's time, input adjust included
    public static EJudgement GetJudgementForLag(CChip chip, int lag)
    {
        STHitRanges ranges = chip.eInstrumentPart switch
        {
            EInstrumentPart.DRUMS => chip.nChannelNumber is EChannel.BassDrum or EChannel.LeftPedal or EChannel.LeftBassDrum
                ? CDTXMania.stDrumPedalHitRanges
                : CDTXMania.stDrumHitRanges,
            EInstrumentPart.GUITAR => CDTXMania.stGuitarHitRanges,
            EInstrumentPart.BASS => CDTXMania.stBassHitRanges,
            _ => STHitRanges.tCreateDefaultDTXHitRanges()
        };
        return ranges.tGetJudgement(Math.Abs(lag));
    }
}
