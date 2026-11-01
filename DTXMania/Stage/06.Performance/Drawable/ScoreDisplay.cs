using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania;

public class ScoreDisplay : UIGroup
{
    [AddChildMenu("Game/Performance/Score Display")]
    public new static UIDrawable Create() => new ScoreDisplay();

    private static readonly int probeScore = AllocationProbe.Register("score");
    private static readonly int probeCombo = AllocationProbe.Register("combo");

    private ScoreBonus bonus = null!;

    public ScoreDisplay() : base("Score")
    {
        size = new Vector2(1280, 720);
    }

    internal void AddBonus()
    {
        bonus.Show();
    }

    internal void Setup(CActPerfCommonScore score, CActPerfCommonCombo combo, EInstrumentPart part, float scoreX, float comboX)
    {
        bonus = GetChild<ScoreBonus>("Bonus") ?? AddChild(new ScoreBonus());

        AddPart("Score", probeScore, origin =>
        {
            if (CDTXMania.ConfigIni.bShowScore)
            {
                score.Draw(origin, part);
            }
        }).position.X = scoreX;

        AddPart("Combo", probeCombo, origin => combo.Draw(origin, (int)part)).position.X = comboX;
    }

    private LegacyDrawable AddPart(string partName, int probe, Action<Matrix4x4> draw)
    {
        return AddChild(new LegacyDrawable(partName, origin =>
        {
            AllocationProbe.Begin(probe);
            draw(origin);
            AllocationProbe.End(probe);
        }));
    }
}
