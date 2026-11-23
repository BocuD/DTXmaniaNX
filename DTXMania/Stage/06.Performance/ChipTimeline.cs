using DTXMania.Core;

namespace DTXMania;

public enum HitState
{
    NotHit,
    Hit,
    DontCare
}

public class ChipTimeline
{
    public List<CChip> Chips { get; private set; } = [];

    //first chip still in play, where searches start. -1 when the chart has no chips, Chips.Count once all are done
    public int TopChip { get; private set; } = -1;

    public void Load(List<CChip>? chips)
    {
        Chips = chips ?? [];
        TopChip = Chips.Count > 0 ? 0 : -1;
    }

    //the chip under the cursor needs this frame's distance first
    public bool TryAdvance()
    {
        CChip chip = Chips[TopChip];
        CChip last = chip.bIsLongNote ? chip.chipLongNoteEndPosition : chip;
        if (!chip.bHit || chip.nDistanceFromBar.Drums >= -65 || !last.bHit || last.nDistanceFromBar.Drums >= -65)
        {
            return false;
        }

        TopChip++;
        return true;
    }
    
    public void Seek(long time)
    {
        if (TopChip == -1)
        {
            return;
        }

        TopChip = 0;
        while (TopChip < Chips.Count && Chips[TopChip].nPlaybackTimeMs < time)
        {
            TopChip++;
        }

        for (int i = TopChip; i < Chips.Count; i++)
        {
            Chips[i].bHit = false;
            Chips[i].bIsHittingLongNote = false;
        }
    }

    public void SeekToEnd()
    {
        TopChip = Chips.Count - 1;
    }

    //lane is HiHatClose to LeftBassDrum, includes hidden chips
    public CChip? NearestDrum(long time, EChannel lane, HitState hitState) =>
        Search(time, lane, EInstrumentPart.UNKNOWN, 0, hitState, preferPast: false);

    public CChip? NearestNote(long time, EInstrumentPart part, int rangeMs, HitState hitState, bool preferPast) =>
        Search(time, EChannel.Invalid, part, rangeMs, hitState, preferPast);

    public CChip? Nearest(long time, EChannel channel, int rangeMs, HitState hitState, bool preferPast = true) =>
        Search(time, channel, EInstrumentPart.UNKNOWN, rangeMs, hitState, preferPast);

    //rangeMs 0 is unlimited. preferPast takes the earlier match even when the later one is nearer; otherwise the
    //nearer wins, the earlier on a tie
    private CChip? Search(long time, EChannel channel, EInstrumentPart part, int rangeMs, HitState hitState, bool preferPast)
    {
        if (TopChip == -1)
        {
            return null;
        }

        int count = Chips.Count;
        int later = -1;
        int i = Math.Min(TopChip, count - 1);
        for (; i < count; i++)
        {
            CChip chip = Chips[i];
            if (chip.nPlaybackTimeMs <= time)
            {
                continue;
            }
            if (rangeMs > 0 && chip.nPlaybackTimeMs - time > rangeMs)
            {
                break;
            }
            if (Matches(chip, channel, part, hitState))
            {
                later = i;
                break;
            }
        }

        //whatever its time: the cursor can be past a chip that is still ahead
        int earlier = -1;
        for (i--; i >= 0; i--)
        {
            CChip chip = Chips[i];
            long distance = Math.Abs(time - chip.nPlaybackTimeMs);
            if (rangeMs > 0 && distance > rangeMs)
            {
                if (chip.nPlaybackTimeMs < time)
                {
                    break;
                }
                continue;
            }
            if (Matches(chip, channel, part, hitState))
            {
                earlier = i;
                break;
            }
        }

        if (later == -1)
        {
            return earlier == -1 ? null : Chips[earlier];
        }
        if (earlier == -1)
        {
            return Chips[later];
        }
        if (preferPast || Math.Abs(time - Chips[earlier].nPlaybackTimeMs) <= Chips[later].nPlaybackTimeMs - time)
        {
            return Chips[earlier];
        }
        return Chips[later];
    }

    private static bool Matches(CChip chip, EChannel channel, EInstrumentPart part, HitState hitState)
    {
        if ((hitState == HitState.NotHit && chip.bHit) || (hitState == HitState.Hit && !chip.bHit))
        {
            return false;
        }
        if (part != EInstrumentPart.UNKNOWN)
        {
            return chip.eInstrumentPart == part;
        }
        if (channel is >= EChannel.HiHatClose and <= EChannel.LeftBassDrum)
        {
            return chip.nChannelNumber == channel || chip.nChannelNumber == channel + (EChannel.HiHatClose_Hidden - EChannel.HiHatClose);
        }
        return chip.nChannelNumber == channel;
    }
}
