using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class KeyAssignPage : ConfigPage
{
    private readonly (EKeyConfigPart part, EKeyConfigPad pad, string label)[] pads;
    private readonly bool includeMidiTest;

    private KeyAssignPage(ConfigList list, (EKeyConfigPart, EKeyConfigPad, string)[] pads, bool includeMidiTest = false) : base(list)
    {
        this.pads = pads;
        this.includeMidiTest = includeMidiTest;
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [BackItem()];

        items.Add(new CItemBase(CDTXMania.isJapanese ? "入力テスト" : "Input Test", CItemBase.EPanelType.Folder,
            "現在割り当てられている入力をテストします。",
            "Test your currently mapped inputs.")
        {
            action = () => list.onOpenInputTest?.Invoke(pads)
        });

        if (includeMidiTest)
        {
            items.Add(new CItemBase(CDTXMania.isJapanese ? "MIDI テスト" : "MIDI Test", CItemBase.EPanelType.Folder,
                "未割り当てのMIDI入力がないか確認できます。",
                "Check for any MIDI inputs that aren't mapped.")
            {
                action = () => list.onOpenMidiTest?.Invoke()
            });
        }

        foreach ((EKeyConfigPart part, EKeyConfigPad pad, string label) in pads)
        {
            string jp = $"{label} のキー割り当てを設定します。";
            string en = $"Assign inputs for {label}.";
            items.Add(new CItemBase(label, CItemBase.EPanelType.Normal, jp, en)
            {
                action = () => list.onOpenKeyAssign?.Invoke(part, pad, label),
                // live so the description reflects edits made in the panel as soon as we return
                formatValue = () => CurrentMapping(part, pad, false),
                formatDescription = () => $"{(CDTXMania.isJapanese ? jp : en)}\n\n{CurrentMapping(part, pad)}"
            });
        }

        return items;
    }

    // "Current: Key Z, Key X" for the pad's assigned inputs (or "none").
    private static string CurrentMapping(EKeyConfigPart part, EKeyConfigPad pad, bool addLabel = true)
    {
        List<string> labels = CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad]
            .Where(s => s.InputDevice != EInputDevice.Unknown)
            .Select(KeyCodeNames.FormatBinding)
            .ToList();

        string mapping = labels.Count == 0
            ? (CDTXMania.isJapanese ? "なし" : "none")
            : string.Join(", ", labels);

        if (!addLabel) return mapping;

        return CDTXMania.isJapanese ? $"現在: {mapping}" : $"Current: {mapping}";
    }
    
    public static KeyAssignPage ForDrums(ConfigList list) => new(list, DrumsPads, includeMidiTest: true);
    public static KeyAssignPage ForGuitar(ConfigList list) => new(list, InstrumentPads(EKeyConfigPart.GUITAR));
    public static KeyAssignPage ForBass(ConfigList list) => new(list, InstrumentPads(EKeyConfigPart.BASS));
    public static KeyAssignPage ForSystem(ConfigList list) => new(list, SystemPads);

    private static readonly (EKeyConfigPart, EKeyConfigPad, string)[] DrumsPads =
    [
        (EKeyConfigPart.DRUMS, EKeyConfigPad.LC, "LeftCymbal"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.HH, "HiHat(Close)"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.HHO, "HiHat(Open)"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.SD, "Snare"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.BD, "Bass"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.HT, "HighTom"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.LT, "LowTom"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.FT, "FloorTom"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.CY, "RightCymbal"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.RD, "RideCymbal"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.LP, "LeftPedal"),
        (EKeyConfigPart.DRUMS, EKeyConfigPad.LBD, "LeftBassDrum"),
    ];

    private static (EKeyConfigPart, EKeyConfigPad, string)[] InstrumentPads(EKeyConfigPart part) =>
    [
        (part, EKeyConfigPad.R, "R"),
        (part, EKeyConfigPad.G, "G"),
        (part, EKeyConfigPad.B, "B"),
        (part, EKeyConfigPad.Y, "Y"),
        (part, EKeyConfigPad.P, "P"),
        (part, EKeyConfigPad.Pick, "Pick"),
        (part, EKeyConfigPad.Wail, "Wailing"),
        (part, EKeyConfigPad.Decide, "Decide"),
        (part, EKeyConfigPad.Cancel, "Cancel"),
    ];

    private static readonly (EKeyConfigPart, EKeyConfigPad, string)[] SystemPads =
    [
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.Capture, "Capture"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.Search, "Search"),
        (EKeyConfigPart.GUITAR, EKeyConfigPad.Help, "Help"),
        (EKeyConfigPart.BASS, EKeyConfigPad.Help, "Pause"), //there is actually a reason for this, we can't fix this mess yet
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopCreate, "Loop Create"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopDelete, "Loop Delete"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipForward, "Skip forward"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipBackward, "Skip backward"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.IncreasePlaySpeed, "Increase play speed"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.DecreasePlaySpeed, "Decrease play speed"),
        (EKeyConfigPart.SYSTEM, EKeyConfigPad.Restart, "Restart"),
    ];
}
