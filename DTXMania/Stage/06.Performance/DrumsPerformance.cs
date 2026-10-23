using DTXMania.Core;
using FDK;

namespace DTXMania;

internal enum EDrumsEvent
{
    AutoNote,
    Hit,
    EmptyHit,
    Bonus,
    Judged
}

internal struct DrumsEvent
{
    public EDrumsEvent Kind;

    public CChip? Chip;

    public long Time;

    public int Lane;

    //a cell of the pad art
    public int Pad;

    //0 to 1
    public float Velocity;

    public CChip? Sound;

    public bool Fire;

    public EJudgement Judgement;

    public bool Auto;

    public bool Miss;

    public bool CountsCombo;
}

internal class DrumsPerformance
{
    //by channel from HiHatClose to LeftBassDrum
    public static readonly int[] ChannelLanes = [1, 2, 3, 4, 5, 7, 6, 1, 9, 0, 8, 8];
    private static readonly int[] ChannelPads = [1, 2, 3, 4, 5, 7, 6, 1, 8, 0, 9, 9];

    //by EPad from HH to LBD
    private static readonly int[] PadLanes = [1, 2, 3, 4, 5, 6, 7, 1, 9, 0, 8, 8];
    private static readonly int[] PadArt = [1, 2, 3, 4, 5, 6, 7, 1, 8, 0, 9, 9];
    public static readonly EChannel[] PadChannels =
    [
        EChannel.HiHatClose, EChannel.Snare, EChannel.BassDrum, EChannel.HighTom, EChannel.LowTom, EChannel.FloorTom,
        EChannel.Cymbal, EChannel.HiHatOpen, EChannel.RideCymbal, EChannel.LeftCymbal, EChannel.LeftPedal, EChannel.LeftBassDrum
    ];

    //by no-chip channel from HiHatClose_NoChip; the guitar and bass ones sit in between
    private static readonly EPad[] EmptyHitChannelPads =
    [
        EPad.HH, EPad.SD, EPad.BD, EPad.HT, EPad.LT, EPad.CY, EPad.FT, EPad.HHO, EPad.RD,
        EPad.UNKNOWN, EPad.UNKNOWN, EPad.LC, EPad.LP, EPad.LBD
    ];

    private const int PadCount = (int)EPad.LBD + 1;

    public readonly PerformanceData Data;
    public readonly List<DrumsEvent> Events = [];

    //by EPad
    public readonly CChip?[] EmptyHitSounds = new CChip?[PadCount];

    public bool HadInput { get; private set; }

    private readonly ChipTimeline timeline;
    private readonly STAUTOPLAY auto;
    private readonly List<STInputEvent> padEvents = [];
    private readonly List<EChannel> candidates = [];
    private readonly List<CChip> candidateChips = [];

    public DrumsPerformance(PerformanceData data, ChipTimeline timeline, STAUTOPLAY auto)
    {
        Data = data;
        this.timeline = timeline;
        this.auto = auto;
    }

    private static int InputAdjustMs => CDTXMania.ConfigIni.nInputAdjustTimeMs.Drums;

    public static int LaneOf(CChip chip) => ChannelLanes[chip.nChannelNumber - EChannel.HiHatClose];

    private static int PadOf(CChip chip) => ChannelPads[chip.nChannelNumber - EChannel.HiHatClose];

    private bool IsAuto(CChip chip) => auto[LaneOf(chip)];

    private int AdjustFor(CChip chip) => IsAuto(chip) ? 0 : InputAdjustMs;

    //correctLane false is a miss without judging the timing
    private void ApplyHit(CChip chip, long time, bool correctLane)
    {
        chip.bHit = true;
        bool isAuto = IsAuto(chip);
        chip.bIsAutoPlayed = isAuto;
        EJudgement judgement = correctLane ? Data.JudgeChip(time, chip, isAuto ? 0 : InputAdjustMs) : EJudgement.Miss;

        //before ApplyJudgement, so the last perfect's top-up to 1,000,000 includes the bonus score
        if (judgement is EJudgement.Great or EJudgement.Perfect && chip.bBonusChip)
        {
            Events.Add(new DrumsEvent { Kind = EDrumsEvent.Bonus, Chip = chip });
            if (CDTXMania.ConfigIni.nSkillMode == 1 && (!CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || CDTXMania.ConfigIni.bAutoAddGage))
            {
                Data.AddScore(500L);
            }
        }

        bool countsCombo = CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || !isAuto;
        Data.ApplyJudgement(judgement, isAuto, chip.nLag, countsCombo);
        Events.Add(new DrumsEvent
        {
            Kind = EDrumsEvent.Judged, Chip = chip, Lane = LaneOf(chip), Judgement = judgement, Auto = isAuto, CountsCombo = countsCombo
        });
    }

    public void UpdateNote(CChip chip)
    {
        long now = AudioMixer.Timer.nCurrentTime;
        bool isAuto = IsAuto(chip);
        if (!chip.bHit && chip.nDistanceFromBar.Drums < 0 && Data.JudgeChip(now, chip, isAuto ? 0 : InputAdjustMs) == EJudgement.Miss)
        {
            ApplyHit(chip, now, true);
        }

        if (chip.bHit || !isAuto)
        {
            return;
        }

        long ghostLag = 0;
        List<int>? ghost = CDTXMania.listAutoGhostLag.Drums;
        if (CDTXMania.ConfigIni.eAutoGhost.Drums != EAutoGhostData.PERFECT && ghost != null &&
            0 <= chip.n楽器パートでの出現順 && chip.n楽器パートでの出現順 < ghost.Count)
        {
            ghostLag = (ghost[chip.n楽器パートでの出現順] & 255) - 128 - InputAdjustMs;
            if (chip.nPlaybackTimeMs + ghostLag > now)
            {
                return;
            }
        }
        else if (chip.nDistanceFromBar.Drums >= 0)
        {
            return;
        }

        chip.bHit = true;
        long time = chip.nPlaybackTimeMs + ghostLag;
        Events.Add(new DrumsEvent
        {
            Kind = EDrumsEvent.AutoNote, Chip = chip, Time = time, Lane = LaneOf(chip), Pad = PadOf(chip), Velocity = CInputManager.nDefaultVelocity / 127f
        });
        ApplyHit(chip, time, true);
    }

    //the chip turns into a plain chip of its lane once it has passed
    public void UpdateEmptyHitChip(CChip chip)
    {
        if (chip.bHit || chip.nDistanceFromBar.Drums >= 0)
        {
            return;
        }

        chip.bHit = true;
        EPad pad = EmptyHitChannelPads[chip.nChannelNumber - EChannel.HiHatClose_NoChip];
        EmptyHitSounds[(int)pad] = chip;
        chip.nChannelNumber = chip.nChannelNumber < EChannel.LeftCymbal_NoChip
            ? chip.nChannelNumber - EChannel.HiHatClose_NoChip + EChannel.HiHatClose
            : chip.nChannelNumber - EChannel.LeftCymbal_NoChip + EChannel.LeftCymbal;
    }

    public void UpdateInput()
    {
        HadInput = false;
        CConfigIni config = CDTXMania.ConfigIni;

        //groupings over a lane the chart does not use fall back to sharing it
        EHHGroup hhGroup = config.eHHGroup;
        ECYGroup cyGroup = config.eCYGroup;
        if (!CDTXMania.DTX.bHasChips.Ride && cyGroup == ECYGroup.打ち分ける)
        {
            cyGroup = ECYGroup.共通;
        }
        if (!CDTXMania.DTX.bHasChips.HHOpen && hhGroup == EHHGroup.全部打ち分ける)
        {
            hhGroup = EHHGroup.左シンバルのみ打ち分ける;
        }
        if (!CDTXMania.DTX.bHasChips.HHOpen && hhGroup == EHHGroup.ハイハットのみ打ち分ける)
        {
            hhGroup = EHHGroup.全部共通;
        }
        if (!CDTXMania.DTX.bHasChips.LeftCymbal && hhGroup == EHHGroup.全部打ち分ける)
        {
            hhGroup = EHHGroup.ハイハットのみ打ち分ける;
        }
        if (!CDTXMania.DTX.bHasChips.LeftCymbal && hhGroup == EHHGroup.左シンバルのみ打ち分ける)
        {
            hhGroup = EHHGroup.全部共通;
        }

        for (int pad = 0; pad < PadCount; pad++)
        {
            CDTXMania.Pad.GetEvents(EInstrumentPart.DRUMS, (EPad)pad, padEvents);
            if (padEvents.Count == 0)
            {
                continue;
            }

            HadInput = true;
            foreach (STInputEvent input in padEvents)
            {
                //electronic kits send quiet crosstalk on purpose
                if (!input.b押された || input.nVelocity <= MinVelocity((EPad)pad))
                {
                    continue;
                }

                long time = input.nTimeStamp - AudioMixer.Timer.nResetAtMs;
                float velocity = input.nVelocity / 127f;
                if (!HitPad((EPad)pad, time, velocity, hhGroup, cyGroup))
                {
                    HitEmpty((EPad)pad, time, velocity);
                }
            }
        }
    }

    private static int MinVelocity(EPad pad)
    {
        CConfigIni.STLANEVALUE min = CDTXMania.ConfigIni.nVelocityMin;
        return pad switch
        {
            EPad.HH or EPad.HHO => min.HH,
            EPad.SD => min.SD,
            EPad.BD => min.BD,
            EPad.HT => min.HT,
            EPad.LT => min.LT,
            EPad.FT => min.FT,
            EPad.CY => min.CY,
            EPad.RD => min.RD,
            EPad.LC => min.LC,
            EPad.LP => min.LP,
            _ => min.LBD
        };
    }

    private static bool IsPedal(EPad pad) => pad is EPad.BD or EPad.LP or EPad.LBD;

    private int PadAdjustMs(EPad pad) =>
        (auto[PadLanes[(int)pad]] ? 0 : InputAdjustMs) + (IsPedal(pad) ? CDTXMania.ConfigIni.nPedalLagTime : 0);

    //the lanes a pad can hit, in the order ties are hit
    private void FillCandidates(EPad pad, EHHGroup hhGroup, ECYGroup cyGroup)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        bool free = config.bシンバルフリー;
        candidates.Clear();
        switch (pad)
        {
            case EPad.HH:
                candidates.Add(EChannel.HiHatClose);
                if (hhGroup is EHHGroup.左シンバルのみ打ち分ける or EHHGroup.全部共通) candidates.Add(EChannel.HiHatOpen);
                if (hhGroup is EHHGroup.ハイハットのみ打ち分ける or EHHGroup.全部共通) candidates.Add(EChannel.LeftCymbal);
                break;

            case EPad.HHO:
                if (hhGroup is EHHGroup.左シンバルのみ打ち分ける or EHHGroup.全部共通) candidates.Add(EChannel.HiHatClose);
                candidates.Add(EChannel.HiHatOpen);
                if (hhGroup is EHHGroup.ハイハットのみ打ち分ける or EHHGroup.全部共通) candidates.Add(EChannel.LeftCymbal);
                break;

            case EPad.BD:
                switch (config.eBDGroup)
                {
                    case EBDGroup.BDとLPで打ち分ける:
                        candidates.AddRange([EChannel.BassDrum, EChannel.LeftBassDrum]);
                        break;
                    case EBDGroup.どっちもBD:
                        candidates.AddRange([EChannel.LeftPedal, EChannel.LeftBassDrum, EChannel.BassDrum]);
                        break;
                    default:
                        candidates.Add(EChannel.BassDrum);
                        break;
                }
                break;

            case EPad.LP:
                switch (config.eBDGroup)
                {
                    case EBDGroup.左右ペダルのみ打ち分ける:
                        candidates.AddRange([EChannel.LeftPedal, EChannel.LeftBassDrum]);
                        break;
                    case EBDGroup.どっちもBD:
                        candidates.AddRange([EChannel.LeftPedal, EChannel.LeftBassDrum, EChannel.BassDrum]);
                        break;
                    default:
                        candidates.Add(EChannel.LeftPedal);
                        break;
                }
                break;

            case EPad.LBD:
                switch (config.eBDGroup)
                {
                    case EBDGroup.BDとLPで打ち分ける:
                        candidates.AddRange([EChannel.BassDrum, EChannel.LeftBassDrum]);
                        break;
                    case EBDGroup.左右ペダルのみ打ち分ける:
                        candidates.AddRange([EChannel.LeftPedal, EChannel.LeftBassDrum]);
                        break;
                    case EBDGroup.どっちもBD:
                        candidates.AddRange([EChannel.LeftPedal, EChannel.LeftBassDrum, EChannel.BassDrum]);
                        break;
                    default:
                        candidates.Add(EChannel.LeftBassDrum);
                        break;
                }
                break;

            case EPad.LT:
            case EPad.FT:
                if (config.eFTGroup == EFTGroup.共通)
                {
                    candidates.AddRange([EChannel.LowTom, EChannel.FloorTom]);
                }
                else
                {
                    candidates.Add(pad == EPad.LT ? EChannel.LowTom : EChannel.FloorTom);
                }
                break;

            case EPad.CY:
                candidates.Add(EChannel.Cymbal);
                if (cyGroup == ECYGroup.共通) candidates.Add(EChannel.RideCymbal);
                if (free) candidates.Add(EChannel.LeftCymbal);
                break;

            case EPad.RD:
                if (cyGroup == ECYGroup.打ち分ける)
                {
                    candidates.Add(EChannel.RideCymbal);
                    break;
                }
                candidates.AddRange([EChannel.Cymbal, EChannel.RideCymbal]);
                if (free) candidates.Add(EChannel.LeftCymbal);
                break;

            case EPad.LC:
                bool sharesHiHat = hhGroup is EHHGroup.ハイハットのみ打ち分ける or EHHGroup.全部共通;
                if (sharesHiHat) candidates.AddRange([EChannel.HiHatClose, EChannel.HiHatOpen]);
                candidates.Add(EChannel.LeftCymbal);
                if (free)
                {
                    candidates.Add(EChannel.Cymbal);
                    if (config.eCYGroup == ECYGroup.共通) candidates.Add(EChannel.RideCymbal);
                }
                break;

            default:
                candidates.Add(PadChannels[(int)pad]);
                break;
        }
    }

    //hits the earliest chip in range; a cymbal pad takes only the first of a tie, the others hit all of it
    private bool HitPad(EPad pad, long time, float velocity, EHHGroup hhGroup, ECYGroup cyGroup)
    {
        FillCandidates(pad, hhGroup, cyGroup);
        int adjust = PadAdjustMs(pad);
        candidateChips.Clear();
        int earliest = int.MaxValue;
        foreach (EChannel lane in candidates)
        {
            CChip? chip = timeline.NearestDrum(time + adjust, lane, HitState.NotHit);
            if (chip != null && Data.JudgeChip(time, chip, adjust) != EJudgement.Miss)
            {
                candidateChips.Add(chip);
                earliest = Math.Min(earliest, chip.nPlaybackPosition);
            }
        }

        if (candidateChips.Count == 0)
        {
            return false;
        }

        bool cymbal = pad is EPad.CY or EPad.RD or EPad.LC;
        EPad soundPad = pad == EPad.RD && cyGroup == ECYGroup.共通 ? EPad.CY : pad;
        foreach (CChip chip in candidateChips)
        {
            if (chip.nPlaybackPosition == earliest)
            {
                HitChip(chip, time, velocity, soundPad);
                if (cymbal)
                {
                    break;
                }
            }
        }
        return true;
    }

    private void HitChip(CChip chip, long time, float velocity, EPad soundPad)
    {
        int adjust = AdjustFor(chip);
        EJudgement judgement = Data.JudgeChip(time, chip, adjust);
        if (judgement == EJudgement.Miss)
        {
            return;
        }

        ApplyHit(chip, time, true);
        Events.Add(new DrumsEvent
        {
            Kind = EDrumsEvent.Hit,
            Chip = chip,
            Lane = LaneOf(chip),
            Pad = PadOf(chip),
            Velocity = velocity,
            Fire = judgement != EJudgement.Poor,
            Sound = CDTXMania.ConfigIni.bドラム打音を発声する ? FindHitSound(chip, soundPad, time + adjust) : null
        });
    }

    private static bool ChipSoundFirst(EPad pad)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        EPlaybackPriority priority = pad switch
        {
            EPad.HH or EPad.HHO or EPad.LC => config.eHitSoundPriorityHH,
            EPad.LT or EPad.FT => config.eHitSoundPriorityFT,
            EPad.CY or EPad.RD => config.eHitSoundPriorityCY,
            EPad.BD or EPad.LP or EPad.LBD => config.eHitSoundPriorityLP,
            _ => EPlaybackPriority.ChipOverPadPriority
        };
        return priority == EPlaybackPriority.ChipOverPadPriority;
    }

    private CChip FindHitSound(CChip chip, EPad pad, long time)
    {
        if (ChipSoundFirst(pad))
        {
            return chip;
        }

        if (pad == EPad.HHO && !CDTXMania.DTX.bHasChips.HHOpen)
        {
            pad = EPad.HH;
        }
        else if (pad == EPad.RD && !CDTXMania.DTX.bHasChips.Ride)
        {
            pad = EPad.CY;
        }
        else if (pad == EPad.LC && !CDTXMania.DTX.bHasChips.LeftCymbal)
        {
            pad = EPad.HH;
        }
        return timeline.NearestDrum(time, PadChannels[(int)pad], HitState.DontCare) ?? chip;
    }

    private void HitEmpty(EPad pad, long time, float velocity)
    {
        int lane = PadLanes[(int)pad];
        CChip? sound = null;
        if (CDTXMania.ConfigIni.bドラム打音を発声する)
        {
            sound = FindEmptyHitSound(pad) ?? FindNearestSound(pad, time + PadAdjustMs(pad));
        }

        bool miss = CDTXMania.ConfigIni.bTight;
        if (miss)
        {
            Data.Damage(EJudgement.Miss);
            Data.BreakCombo();
        }

        Events.Add(new DrumsEvent
        {
            Kind = EDrumsEvent.EmptyHit, Lane = lane, Pad = PadArt[(int)pad], Velocity = velocity, Sound = sound, Miss = miss, Auto = auto[lane]
        });
    }

    public CChip? FindEmptyHitSound(EPad pad)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        CChip?[] sounds = EmptyHitSounds;
        switch (pad)
        {
            case EPad.HH:
                if (sounds[(int)EPad.HH] != null) return sounds[(int)EPad.HH];
                if (config.eHHGroup == EHHGroup.左シンバルのみ打ち分ける) return sounds[(int)EPad.HHO];
                if (config.eHHGroup != EHHGroup.ハイハットのみ打ち分ける && sounds[(int)EPad.HHO] != null) return sounds[(int)EPad.HHO];
                return sounds[(int)EPad.LC];

            case EPad.HHO:
                if (sounds[(int)EPad.HHO] != null) return sounds[(int)EPad.HHO];
                if (config.eHHGroup == EHHGroup.左シンバルのみ打ち分ける) return sounds[(int)EPad.HH];
                if (config.eHHGroup != EHHGroup.ハイハットのみ打ち分ける && sounds[(int)EPad.HH] != null) return sounds[(int)EPad.HH];
                return sounds[(int)EPad.LC];

            case EPad.LT:
                return sounds[(int)EPad.LT] ?? (config.eFTGroup == EFTGroup.共通 ? sounds[(int)EPad.FT] : null);

            case EPad.FT:
                return sounds[(int)EPad.FT] ?? (config.eFTGroup == EFTGroup.共通 ? sounds[(int)EPad.LT] : null);

            case EPad.CY:
                return sounds[(int)EPad.CY] ?? (config.eCYGroup == ECYGroup.共通 ? sounds[(int)EPad.RD] : null);

            case EPad.RD:
                return sounds[(int)EPad.RD] ?? (config.eCYGroup == ECYGroup.共通 ? sounds[(int)EPad.CY] : null);

            case EPad.LC:
                if (sounds[(int)EPad.LC] != null) return sounds[(int)EPad.LC];
                if (config.eHHGroup is not (EHHGroup.ハイハットのみ打ち分ける or EHHGroup.全部共通)) return null;
                return sounds[(int)EPad.HH] ?? sounds[(int)EPad.HHO];

            case EPad.LP:
                return sounds[(int)EPad.LP] ?? (config.eBDGroup != EBDGroup.左右ペダルのみ打ち分ける ? sounds[(int)EPad.LBD] : null);

            case EPad.LBD:
                return sounds[(int)EPad.LBD] ?? (config.eBDGroup != EBDGroup.左右ペダルのみ打ち分ける ? sounds[(int)EPad.LP] : null);

            default:
                return sounds[(int)pad];
        }
    }

    private CChip? FindNearestSound(EPad pad, long time)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        CChip? Near(EChannel lane) => timeline.NearestDrum(time, lane, HitState.DontCare);

        switch (pad)
        {
            case EPad.HH:
                return config.eHHGroup switch
                {
                    EHHGroup.ハイハットのみ打ち分ける => Near(EChannel.HiHatClose) ?? Near(EChannel.LeftCymbal),
                    EHHGroup.左シンバルのみ打ち分ける => Near(EChannel.HiHatClose) ?? Near(EChannel.HiHatOpen),
                    EHHGroup.全部共通 => Near(EChannel.HiHatClose) ?? Earlier(Near(EChannel.HiHatOpen), Near(EChannel.LeftCymbal)),
                    _ => Near(EChannel.HiHatClose)
                };

            case EPad.HHO:
                return config.eHHGroup switch
                {
                    EHHGroup.ハイハットのみ打ち分ける => Near(EChannel.HiHatOpen) ?? Near(EChannel.LeftCymbal),
                    EHHGroup.左シンバルのみ打ち分ける => Near(EChannel.HiHatOpen) ?? Near(EChannel.HiHatClose),
                    EHHGroup.全部共通 => Near(EChannel.HiHatOpen) ?? Earlier(Near(EChannel.HiHatClose), Near(EChannel.LeftCymbal)),
                    _ => Near(EChannel.HiHatOpen)
                };

            case EPad.LC:
                return config.eHHGroup is EHHGroup.ハイハットのみ打ち分ける or EHHGroup.全部共通
                    ? Near(EChannel.LeftCymbal) ?? Earlier(Near(EChannel.HiHatClose), Near(EChannel.HiHatOpen))
                    : Near(EChannel.LeftCymbal);

            case EPad.LT:
                return config.eFTGroup != EFTGroup.打ち分ける ? Near(EChannel.LowTom) ?? Near(EChannel.FloorTom) : Near(EChannel.LowTom);

            case EPad.FT:
                return config.eFTGroup != EFTGroup.打ち分ける ? Near(EChannel.FloorTom) ?? Near(EChannel.LowTom) : Near(EChannel.FloorTom);

            case EPad.CY:
                return config.eCYGroup != ECYGroup.打ち分ける ? Near(EChannel.Cymbal) ?? Near(EChannel.RideCymbal) : Near(EChannel.Cymbal);

            case EPad.RD:
                return config.eCYGroup != ECYGroup.打ち分ける ? Near(EChannel.RideCymbal) ?? Near(EChannel.Cymbal) : Near(EChannel.RideCymbal);

            case EPad.BD:
            {
                CChip? bd = Near(EChannel.BassDrum);
                CChip? lbd = Near(EChannel.LeftBassDrum);
                return config.eBDGroup switch
                {
                    EBDGroup.左右ペダルのみ打ち分ける => bd ?? Near(EChannel.LeftPedal),
                    EBDGroup.BDとLPで打ち分ける => bd != null && lbd != null ? (bd.nPlaybackTimeMs >= lbd.nPlaybackTimeMs ? bd : lbd) : lbd ?? bd,
                    EBDGroup.どっちもBD => bd ?? Earlier(Near(EChannel.LeftPedal), lbd),
                    _ => bd
                };
            }

            case EPad.LP:
                return config.eBDGroup switch
                {
                    EBDGroup.左右ペダルのみ打ち分ける => Near(EChannel.LeftPedal) ?? Near(EChannel.LeftBassDrum),
                    EBDGroup.どっちもBD => Near(EChannel.LeftPedal) ?? Earlier(Near(EChannel.LeftBassDrum), Near(EChannel.BassDrum)),
                    _ => Near(EChannel.LeftPedal)
                };

            case EPad.LBD:
            {
                CChip? bd = Near(EChannel.BassDrum);
                CChip? lbd = Near(EChannel.LeftBassDrum);
                return config.eBDGroup switch
                {
                    EBDGroup.左右ペダルのみ打ち分ける => lbd ?? bd,
                    EBDGroup.BDとLPで打ち分ける => bd != null && lbd != null ? (bd.nPlaybackTimeMs <= lbd.nPlaybackTimeMs ? lbd : bd) : lbd ?? bd,
                    EBDGroup.どっちもBD => lbd ?? Earlier(Near(EChannel.LeftPedal), bd),
                    _ => lbd
                };
            }

            default:
                return Near(PadChannels[(int)pad]);
        }
    }

    //the second one on a tie
    private static CChip? Earlier(CChip? a, CChip? b) =>
        a == null ? b : b == null ? a : a.nPlaybackPosition < b.nPlaybackPosition ? a : b;
}
