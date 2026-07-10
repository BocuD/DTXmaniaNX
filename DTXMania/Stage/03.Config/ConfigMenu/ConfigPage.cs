using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>
/// One page of settings shown in a <see cref="ConfigList"/>. Each concrete page builds its rows in
/// <see cref="Build"/> (a copy of the corresponding old CActConfigList setup method), reusing the
/// shared helpers below. Pages with deferred, apply-on-exit behaviour (audio device, skin) override
/// <see cref="CacheInitialState"/> / <see cref="ApplyPendingChanges"/>.
/// </summary>
internal abstract class ConfigPage
{
    protected readonly ConfigList list;

    protected ConfigPage(ConfigList list)
    {
        this.list = list;
    }

    /// <summary>Builds this page's rows. Called each time the page is (re)opened.</summary>
    public abstract List<CItemBase> Build();

    /// <summary>Snapshot any state needed to detect changes on exit. Called once at Config entry.</summary>
    public virtual void CacheInitialState()
    {
    }

    /// <summary>Apply any deferred changes; Called once when leaving Config.</summary>
    public virtual void ApplyPendingChanges()
    {
    }

    // "<< Back" row; at the root page Back() falls through to the list's onExitRoot (returns to the menu).
    protected CItemBase BackItem(Action? action = null)
    {
        return new CItemBase("<< Back", CItemBase.EPanelType.Return,
            "前のメニューに戻ります。",
            "Return to the previous menu.")
        {
            action = action ?? list.Back
        };
    }

    //A folder row that opens another page as a sub-page (its Build() runs when entered).
    protected CItemBase FolderItem(string name, string descriptionJp, string descriptionEn, ConfigPage target)
    {
        return new CItemBase(name, CItemBase.EPanelType.Folder, descriptionJp, descriptionEn)
        {
            action = () => list.OpenFolder(target.Build()),
            formatValue = () => CDTXMania.isJapanese ? "開く" : "Open Folder"
        };
    }

    // ---- item factories: build a CItem bound to a config getter/setter (keeps pages terse) ----
    protected static CItemToggle Toggle(string name, string jp, string en, Func<bool> get, Action<bool> set)
    {
        CItemToggle item = new(name, get(), jp, en);
        item.BindConfig(() => item.bON = get(), () => set(item.bON));
        return item;
    }

    protected static CItemInteger Integer(string name, int min, int max, string jp, string en, Func<int> get, Action<int> set)
    {
        CItemInteger item = new(name, min, max, get(), jp, en);
        item.BindConfig(() => item.nCurrentValue = get(), () => set(item.nCurrentValue));
        return item;
    }

    protected static CItemList Choice(string name, string jp, string en, string[] values, Func<int> get, Action<int> set)
    {
        CItemList item = new(name, CItemBase.EPanelType.Normal, get(), jp, en, values);
        item.BindConfig(() => item.nCurrentlySelectedIndex = get(), () => set(item.nCurrentlySelectedIndex));
        return item;
    }

    /// <summary>
    /// A choice bound directly to an enum field. Options are auto-populated from the enum's members
    /// (in declaration order), labelled via <see cref="EnumLabelAttribute"/> or the member name
    /// Handles non-contiguous enum values by mapping value ↔ list index
    /// </summary>
    protected static CItemList EnumChoice<TEnum>(string name, string jp, string en, Func<TEnum> get, Action<TEnum> set)
        where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        string[] labels = Array.ConvertAll(values, v => EnumLabels.Get(v));

        int IndexOfCurrent() => Math.Max(0, Array.IndexOf(values, get()));

        CItemList item = new(name, CItemBase.EPanelType.Normal, IndexOfCurrent(), jp, en, labels);
        item.BindConfig(
            () => item.nCurrentlySelectedIndex = IndexOfCurrent(),
            () => set(values[item.nCurrentlySelectedIndex]));
        return item;
    }

    protected static CItemTextInput TextInput(string name, string initialValue, string jp, string en, Func<string> get, Action<string> set)
    {
        CItemTextInput item = new(name, initialValue, jp, en);
        item.BindConfig(() => item.strCurrentValue = get(), () => set(item.strCurrentValue));
        return item;
    }
}
