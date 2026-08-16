using DTXMania.Core;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// App-wide state exposed on the global data context, so any layout can bind to e.g. <c>"Game.Version"</c>.
/// Registered on <see cref="UIDataContext.Global"/> at startup.
/// </summary>
public sealed class GameInfo
{
    //set once at startup; the rest are live properties read on each access
    [DataField] public string Version { get; set; } = string.Empty;
    [DataField] public string VersionDisplay { get; set; } = string.Empty;

    [DataField] public bool IsJapanese => CDTXMania.isJapanese;

    //0 = drums, 1 = guitar, 2 = bass
    [DataField] public int CurrentInstrument => CDTXMania.GetCurrentInstrument();

    [DataField] public int Fps => CDTXMania.FPS?.nCurrentFPS ?? 0;
}
