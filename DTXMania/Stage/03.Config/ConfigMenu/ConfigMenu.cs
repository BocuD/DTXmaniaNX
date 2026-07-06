using DTXMania.UI.Config;

namespace DTXMania;

/// <summary>
/// Owns the config pages for the new <see cref="ConfigList"/> and the cross-page apply-on-exit
/// behaviour. Each page is a <see cref="ConfigPage"/> subclass; this class just wires them together
/// and exposes the entry points the config stage's left menu calls.
/// </summary>
internal class ConfigMenu
{
    private readonly ConfigList list;

    private readonly SystemConfigPage system;
    private readonly DrumsConfigPage drums;
    private readonly GuitarConfigPage guitar;
    private readonly BassConfigPage bass;

    // pages that participate in apply-on-exit / initial-state snapshotting
    private readonly List<ConfigPage> pages;

    public ConfigMenu(ConfigList list)
    {
        this.list = list;

        GraphicsConfigPage graphics = new(list);
        SkinConfigPage skin = new(list);
        AudioConfigPage audio = new(list);
        GameplayConfigPage gameplay = new(list);
        MenuConfigPage menu = new(list);
        system = new SystemConfigPage(list, graphics, skin, audio, gameplay, menu);

        drums = new DrumsConfigPage(list);
        guitar = new GuitarConfigPage(list);
        bass = new BassConfigPage(list);

        pages = [system, graphics, skin, audio, gameplay, menu];

        foreach (ConfigPage page in pages)
        {
            page.CacheInitialState();
        }
    }

    // entry points selected from the left menu (instrument pages are placeholders for now)
    public void OpenSystem() => list.SetItems(system.Build());
    public void OpenDrums() => list.SetItems(drums.Build());
    public void OpenGuitar() => list.SetItems(guitar.Build());
    public void OpenBass() => list.SetItems(bass.Build());

    /// <summary>Applies all deferred changes (sound device, skin). Call once when leaving Config.</summary>
    public void ApplyPendingChanges()
    {
        foreach (ConfigPage page in pages)
        {
            page.ApplyPendingChanges();
        }
    }
}
