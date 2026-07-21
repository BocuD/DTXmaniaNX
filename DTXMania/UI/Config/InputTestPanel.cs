using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using SlimDXKey = SlimDX.DirectInput.Key;
using STKEYASSIGN = DTXMania.Core.CConfigIni.CKeyAssign.STKEYASSIGN;

namespace DTXMania.UI.Config;

internal sealed class InputTestPanel : UIGroup
{
    private const int MaxChannels = 16;
    private const float RowSpacing = 28f;
    private const float StartY = 56f;
    private const float CounterX = 250f;
    private const float LogX = 360f;

    private static readonly Color4 NormalColor = Color4.White;
    private static readonly Color4 HighlightColor = new(1f, 0.85f, 0.2f, 1f);

    private readonly UIText title;
    private readonly UIText hint;
    private readonly UIText logHeader;
    private readonly ScrollingLog log;

    private readonly UIText[] labels = new UIText[MaxChannels];
    private readonly UIText[] counters = new UIText[MaxChannels];

    private (EKeyConfigPart part, EKeyConfigPad pad, string label)[] channels = [];
    private readonly int[] counts = new int[MaxChannels];
    private readonly int[] shownCounts = new int[MaxChannels];
    private readonly bool[] shownHeld = new bool[MaxChannels];

    public Action? onClose;
    public bool IsOpen => isVisible;

    public InputTestPanel() : base("InputTestPanel")
    {
        dontSerialize = true;

        title = AddChild(new UIText("", 22f));

        hint = AddChild(new UIText("", 15f));
        hint.position = new Vector3(0, 28, 0);
        hint.fillColor = new Color4(0.7f, 0.85f, 1f, 1f);

        for (int i = 0; i < MaxChannels; i++)
        {
            UIText label = AddChild(new UIText("", 18f));
            label.position = new Vector3(20, StartY + i * RowSpacing, 0);
            label.isVisible = false;
            labels[i] = label;

            UIText counter = AddChild(new UIText("", 18f));
            counter.position = new Vector3(CounterX, StartY + i * RowSpacing, 0);
            counter.isVisible = false;
            counters[i] = counter;
        }

        logHeader = AddChild(new UIText("", 15f));
        logHeader.position = new Vector3(LogX, StartY - 24, 0);
        logHeader.fillColor = new Color4(0.7f, 0.85f, 1f, 1f);

        log = AddChild(new ScrollingLog(14));
        log.position = new Vector3(LogX, StartY, 0);
    }

    public void Open((EKeyConfigPart part, EKeyConfigPad pad, string label)[] pads)
    {
        title.SetText(CDTXMania.isJapanese ? "入力テスト（全チャンネル）" : "Input Test — All Channels");
        hint.SetText(CDTXMania.isJapanese
            ? "各入力を押して確認します。 Esc で戻ります。"
            : "Press your inputs to test each channel.  (Esc to return)");
        logHeader.SetText(CDTXMania.isJapanese ? "最近の入力" : "Recent hits");

        int count = Math.Min(pads.Length, MaxChannels);
        channels = pads.Length <= MaxChannels ? pads : pads[..MaxChannels];

        for (int i = 0; i < MaxChannels; i++)
        {
            bool used = i < count;
            labels[i].isVisible = used;
            counters[i].isVisible = used;

            if (used)
            {
                labels[i].SetText(pads[i].label);
                labels[i].fillColor = NormalColor;
                labels[i].MarkDirty();
                counters[i].SetText("x0");
                counters[i].fillColor = NormalColor;
                counters[i].MarkDirty();
            }

            counts[i] = 0;
            shownCounts[i] = 0;
            shownHeld[i] = false;
        }

        log.Clear();
        isVisible = true;
    }

    public void UpdatePreview()
    {
        if (!isVisible) return;

        for (int c = 0; c < channels.Length; c++)
        {
            STKEYASSIGN[] bindings = CDTXMania.ConfigIni.KeyAssign[(int)channels[c].part][(int)channels[c].pad];

            bool held = false;
            foreach (STKEYASSIGN a in bindings)
            {
                if (a.InputDevice == EInputDevice.Unknown) continue;

                if (KeyAssignProbe.IsBindingPressed(a, pressing: true)) held = true;

                if (KeyAssignProbe.IsBindingPressed(a, pressing: false))
                {
                    counts[c]++;
                    log.Add($"{channels[c].label}  <-  {KeyCodeNames.FormatBinding(a)}", HighlightColor);
                }
            }

            if (counts[c] != shownCounts[c] || held != shownHeld[c])
            {
                shownCounts[c] = counts[c];
                shownHeld[c] = held;

                counters[c].SetText($"x{counts[c]}");
                counters[c].fillColor = held ? HighlightColor : NormalColor;
                counters[c].MarkDirty();
                labels[c].fillColor = held ? HighlightColor : NormalColor;
                labels[c].MarkDirty();
            }
        }
    }

    public void PollClose()
    {
        if (isVisible && CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Escape))
        {
            CDTXMania.Skin.soundCancel.tPlay();
            Close();
        }
    }

    private void Close()
    {
        isVisible = false;
        onClose?.Invoke();
    }
}
