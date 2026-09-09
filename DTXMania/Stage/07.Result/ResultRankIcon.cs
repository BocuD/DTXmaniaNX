using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

/// <summary>
/// The big rank letter, its backing plate and the clear/full-combo/excellent badges. All three badges are
/// built and the active one shows through its data-bound visibility, rather than an if/else on the score.
/// </summary>
public class ResultRankIcon : UIGroup
{
    private readonly int instrument;

    //the art for the rank itself, which changes when the rank does. The badges below never do
    private readonly List<UIDrawable> rankArt = [];

    public ResultRankIcon(int instrument)
    {
        this.instrument = instrument;

        name = "ResultRankIcon";
        pivot = new Vector2(0.5f, 0.5f);
        size = new Vector2(420, 510);

        BuildRankArt();

        AddBadge("excellent", "Result.IsExcellent", new Vector3(210, 350, 0), new Vector2(0.5f, 0));
        AddBadge("fullcombo_0", "Result.IsFullCombo", new Vector3(55, 350, 0), Vector2.Zero);
        AddBadge("fullcombo_1", "Result.IsFullCombo", new Vector3(180, 320, 0), Vector2.Zero);
        AddBadge("clear_0", "Result.IsClear", new Vector3(210, 364, 0), new Vector2(0.5f, 0));
        AddBadge("clear_1", "Result.IsClear", new Vector3(210, 420, 0), new Vector2(0.5f, 0));
    }

    /// <summary>Rebuilds the rank art, for a rank that changed without the stage being reloaded.</summary>
    public void Refresh()
    {
        foreach (UIDrawable art in rankArt)
        {
            RemoveChild(art);
            art.Dispose();
        }

        rankArt.Clear();
        BuildRankArt();
    }

    private void BuildRankArt()
    {
        bool allAuto = instrument switch
        {
            0 => CDTXMania.ConfigIni.bAllDrumsAreAutoPlay,
            1 => CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay,
            2 => CDTXMania.ConfigIni.bAllBassAreAutoPlay,
            _ => false
        };

        //all-auto play always shows SS; rank 99 (unknown) is grouped with the lower ranks
        (string icon, string bg, bool isSS)? rank = CDTXMania.StageManager.stageResult.nRankValue[instrument] switch
        {
            0 => ("s", "ss", true),
            1 => ("s", "s", false),
            2 => ("a", "a", false),
            3 => ("b", "b", false),
            4 or 5 or 6 or 99 => allAuto ? ("s", "ss", true) : ("c", "c", false),
            _ => null
        };

        if (rank == null)
        {
            return;
        }

        AddIcon(rank.Value.icon, rank.Value.isSS);
        AddBackground(rank.Value.bg);
    }

    private static BaseTexture LoadRankTexture(string fileName)
        => BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Result\Rank\{fileName}.png"));

    private void AddIcon(string iconName, bool isSS)
    {
        BaseTexture texture = LoadRankTexture($"rank_icon_{iconName}");

        UIImage icon = AddChild(new UIImage(texture));
        rankArt.Add(icon);
        icon.name = "Icon";
        icon.renderOrder = 0;
        icon.position = new Vector3(isSS ? 60 : 132, 150, 0);

        //SS is drawn as two S icons side by side
        if (isSS)
        {
            UIImage second = AddChild(new UIImage(texture));
            rankArt.Add(second);
            second.name = "Icon2";
            second.renderOrder = 1;
            second.position = new Vector3(205, 150, 0);
        }
    }

    private void AddBackground(string bgName)
    {
        UIImage bg = AddChild(new UIImage(LoadRankTexture($"rank_bg_{bgName}")));
        bg.name = "Bg";
        bg.renderOrder = -1;
        bg.pivot = new Vector2(0.5f, 0.5f);
        bg.position = new Vector3(210, 255, 0);
    }

    private void AddBadge(string fileName, string visibleWhen, Vector3 position, Vector2 pivot)
    {
        UIImage badge = AddChild(new UIImage(LoadRankTexture(fileName)));
        badge.name = "Badge" + fileName;
        badge.renderOrder = 1;
        badge.scale = new Vector3(0.66f, 0.66f, 1.0f);
        badge.position = position;
        badge.pivot = pivot;
        badge.bindings.Add(new UIBinding("isVisible", visibleWhen));
    }
}
