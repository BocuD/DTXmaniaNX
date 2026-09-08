using DTXMania.Core;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

public sealed class NameplateData(UIPlayerNameplate owner)
{
    [DataField] public string Name => string.IsNullOrEmpty(CDTXMania.ConfigIni.strCardName[owner.instrument])
        ? "GUEST"
        : CDTXMania.ConfigIni.strCardName[owner.instrument];

    [DataField] public string Title => CDTXMania.ConfigIni.strGroupName[owner.instrument] ?? string.Empty;

    [DataField] public bool HasTitle => Title.Length > 0;

    [DataField] public string Skill => SongDb.SongDb.totalSkill.ToString("0.00");

    [DataField] public bool ShowSkill => owner.displaySkill;
}
