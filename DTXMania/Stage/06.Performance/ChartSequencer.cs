using DTXMania.Core;

namespace DTXMania;

internal enum ESequencerEvent
{
    Bga,
    BgaSwap,
    Video,
    Bpm
}

internal struct SequencerEvent
{
    public ESequencerEvent Kind;

    public CChip Chip;

    public double Bpm;
}

internal class ChartSequencer
{
    public readonly List<SequencerEvent> Events = [];

    //the crowd sound for the end of a fill-in
    public CChip? FillInSound { get; private set; }

    private STLANEVALUE<int> lastWavByLane;
    private readonly int[] lastWavByMutingChannel = new int[50];
    private readonly List<int> lastHiHatWavs = new(16);
    private EChannel lastHiHatChannel;

    public ChartSequencer()
    {
        Array.Fill(lastWavByMutingChannel, -1);
        lastWavByLane.Guitar = -1;
        lastWavByLane.Bass = -1;
    }

    public void UpdateUnplayedNote(CChip chip)
    {
        EInstrumentPart part = chip.eInstrumentPart;
        if (!chip.bHit && chip.nDistanceFromBar[(int)part] < 0)
        {
            chip.bHit = true;
            PlaySound(chip, AudioMixer.Timer.nResetAtMs + chip.nPlaybackTimeMs, part, CDTXMania.DTX.nモニタを考慮した音量(part));
        }
    }

    public void Update(CChip chip)
    {
        float distance = chip.nChannelNumber == EChannel.Guitar_WailingSound ? chip.nDistanceFromBar.Guitar : chip.nDistanceFromBar.Drums;
        if (chip.bHit || distance >= 0)
        {
            return;
        }

        chip.bHit = true;
        CConfigIni config = CDTXMania.ConfigIni;
        CDTX dtx = CDTXMania.DTX;
        long chipTime = AudioMixer.Timer.nResetAtMs + chip.nPlaybackTimeMs;
        switch (chip.nChannelNumber)
        {
            case EChannel.BeatLine:
                if (config.bMetronome)
                {
                    CDTXMania.Skin.soundMetronome.tPlay(40);
                }
                break;

            case EChannel.BGM:
            case EChannel.SE06 or EChannel.SE07 or EChannel.SE08 or EChannel.SE09:
            case EChannel.SE10 or EChannel.SE11 or EChannel.SE12 or EChannel.SE13 or EChannel.SE14 or EChannel.SE15:
            case EChannel.SE16 or EChannel.SE17 or EChannel.SE18 or EChannel.SE19:
            case EChannel.SE20 or EChannel.SE21 or EChannel.SE22 or EChannel.SE23:
            case EChannel.SE30 or EChannel.SE31 or EChannel.SE32:
                if (config.bBGM音を発声する)
                {
                    dtx.tPlayChip(chip, chipTime, (int)ELane.BGM, dtx.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                }
                break;

            //only one sound at a time on each of these
            case EChannel.SE01 or EChannel.SE02 or EChannel.SE03 or EChannel.SE04 or EChannel.SE05:
                if (config.bBGM音を発声する)
                {
                    int muting = chip.nChannelNumber - EChannel.SE01;
                    dtx.tStopPlayingWav(lastWavByMutingChannel[muting]);
                    dtx.tPlayChip(chip, chipTime, (int)ELane.BGM, dtx.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                    lastWavByMutingChannel[muting] = chip.nIntegerValue_InternalNumber;
                }
                break;

            //played as if on HH, CY, RD, LC, guitar and bass
            case EChannel.SE24 or EChannel.SE25 or EChannel.SE26 or EChannel.SE27 or EChannel.SE28 or EChannel.SE29:
            {
                EInstrumentPart part = chip.nChannelNumber switch
                {
                    EChannel.SE28 => EInstrumentPart.GUITAR,
                    EChannel.SE29 => EInstrumentPart.BASS,
                    _ => EInstrumentPart.DRUMS
                };
                PlaySound(chip, chipTime, part, dtx.nモニタを考慮した音量(part));
                break;
            }

            case EChannel.BPM:
                Events.Add(new SequencerEvent { Kind = ESequencerEvent.Bpm, Chip = chip, Bpm = chip.nIntegerValue });
                break;

            case EChannel.BPMEx:
                if (dtx.listBPM.TryGetValue(chip.nIntegerValue_InternalNumber, out CDTX.CBPM? bpm))
                {
                    Events.Add(new SequencerEvent { Kind = ESequencerEvent.Bpm, Chip = chip, Bpm = bpm.dbBPM値 });
                }
                break;

            case EChannel.BGALayer1 or EChannel.BGALayer2 or EChannel.BGALayer3 or EChannel.BGALayer4:
            case EChannel.BGALayer5 or EChannel.BGALayer6 or EChannel.BGALayer7 or EChannel.BGALayer8:
                Events.Add(new SequencerEvent { Kind = ESequencerEvent.Bga, Chip = chip });
                break;

            case EChannel.BGALayer1_Swap or EChannel.BGALayer2_Swap or EChannel.BGALayer3_Swap or EChannel.BGALayer4_Swap:
            case EChannel.BGALayer5_Swap or EChannel.BGALayer6_Swap or EChannel.BGALayer7_Swap or EChannel.BGALayer8_Swap:
                Events.Add(new SequencerEvent { Kind = ESequencerEvent.BgaSwap, Chip = chip });
                break;

            case EChannel.Movie:
            case EChannel.MovieFull:
                Events.Add(new SequencerEvent { Kind = ESequencerEvent.Video, Chip = chip });
                break;

            case EChannel.DrumsFillin:
                FillInSound = chip;
                break;

            case EChannel.MixChannel1_unc:
            case EChannel.MixChannel2_unc:
                if (dtx.listWAV.TryGetValue(chip.nIntegerValue_InternalNumber, out CDTX.CWAV? wav) && wav.clip != null)
                {
                    if (chip.nChannelNumber == EChannel.MixChannel1_unc)
                    {
                        AudioMixer.AttachToMixer(wav.clip);
                    }
                    else
                    {
                        AudioMixer.DetachFromMixer(wav.clip);
                    }
                }
                break;
        }
    }

    public void PlaySound(CChip chip, long systemTimeMs, EInstrumentPart part, int volume, bool monitor = false, bool shiftPitch = false)
    {
        CDTX dtx = CDTXMania.DTX;
        switch (part)
        {
            case EInstrumentPart.DRUMS:
                PlayDrumSound(chip, systemTimeMs, volume, monitor);
                break;

            case EInstrumentPart.GUITAR:
                dtx.tStopPlayingWav(lastWavByLane.Guitar);
                dtx.tPlayChip(chip, systemTimeMs, (int)ELane.Guitar, volume, monitor, shiftPitch);
                lastWavByLane.Guitar = chip.nIntegerValue_InternalNumber;
                break;

            case EInstrumentPart.BASS:
                dtx.tStopPlayingWav(lastWavByLane.Bass);
                dtx.tPlayChip(chip, systemTimeMs, (int)ELane.Bass, volume, monitor, shiftPitch);
                lastWavByLane.Bass = chip.nIntegerValue_InternalNumber;
                break;
        }
    }

    public void StopGuitarSound(EInstrumentPart side) =>
        CDTXMania.DTX.tStopPlayingWav(side == EInstrumentPart.GUITAR ? lastWavByLane.GtPick : lastWavByLane.BsPick);

    private void PlayDrumSound(CChip chip, long systemTimeMs, int volume, bool monitor)
    {
        EChannel index = chip.nChannelNumber;
        if (EChannel.HiHatClose <= index && index <= EChannel.LeftBassDrum)
        {
            index -= 0x11;
        }
        else if (EChannel.HiHatClose_Hidden <= index && index <= EChannel.LeftBassDrum_Hidden)
        {
            index -= EChannel.HiHatClose_Hidden;
        }
        else if (index == EChannel.SE24)
        {
            index = 0;
        }
        else if (EChannel.SE25 <= index && index <= EChannel.SE27)
        {
            EChannel[] cymbals = [EChannel.Cymbal, EChannel.RideCymbal, EChannel.LeftCymbal];
            chip.nChannelNumber = cymbals[chip.nChannelNumber - EChannel.SE25];
            index = (EChannel)(chip.nChannelNumber - EChannel.HiHatClose);
        }
        else
        {
            return;
        }

        int lane = DrumsPerformance.ChannelLanes[(int)index];
        bool muteHiHat = (lane == 1 && index == 0 && lastHiHatChannel != EChannel.HiHatOpen && lastHiHatChannel != EChannel.HiHatOpen_Hidden)
                         || (lane == 8 && index == EChannel.BMS_reserved_0A && lastHiHatChannel != EChannel.HiHatOpen && lastHiHatChannel != EChannel.HiHatOpen_Hidden);
        if (muteHiHat && CDTXMania.ConfigIni.bMutingLP)
        {
            foreach (int wav in lastHiHatWavs)
            {
                CDTXMania.DTX.tStopPlayingWav(wav);
            }
            lastHiHatWavs.Clear();
            lastHiHatChannel = chip.nChannelNumber;
        }

        switch (index)
        {
            case EChannel.Nil:
            case EChannel.BGALayer2:
            case EChannel.Guitar_Open:
            case EChannel.Guitar_RGBxx:
                if (lastHiHatWavs.Count >= 16)
                {
                    lastHiHatWavs.RemoveAt(0);
                }
                if (!lastHiHatWavs.Contains(chip.nIntegerValue_InternalNumber))
                {
                    lastHiHatWavs.Add(chip.nIntegerValue_InternalNumber);
                }
                break;
        }

        CDTXMania.DTX.tPlayChip(chip, systemTimeMs, lane, volume, monitor);

        //by lane, not by channel: LC (0x1A - 0x11 = 9) and the guitar share 9 as an index
        lastWavByLane[lane] = chip.nIntegerValue_InternalNumber;
    }
}
