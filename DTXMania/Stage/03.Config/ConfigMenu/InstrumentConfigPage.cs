using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class InstrumentConfigPage : PlayerConfigPage
{
    private readonly InstrumentOptionsPage options;
    private readonly ConfigPage keyAssign;

    public InstrumentConfigPage(ConfigList list, EPlayer player) : base(list, player)
    {
        options = InstrumentOptionsPage.For(list, player);

        keyAssign = player switch
        {
            EPlayer.Drums => KeyAssignPage.ForDrums(list),
            EPlayer.Guitar1 => KeyAssignPage.ForGuitar(list),
            _ => KeyAssignPage.ForBass(list)
        };
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items =
        [
            BackItem(),
            CreateCardNameInputItem(),
            CreateGroupNameInputItem()
        ];

        items.AddRange(options.Build());

        items.Add(FolderItem("Key Assignment", KeyAssignJp, KeyAssignEn, keyAssign));

        return items;
    }

    private string KeyAssignJp => player switch
    {
        EPlayer.Drums => "ドラムのキー割り当てを設定します。",
        EPlayer.Guitar1 => "ギターのキー割り当てを設定します。",
        _ => "ベースのキー割り当てを設定します。"
    };

    private string KeyAssignEn => player switch
    {
        EPlayer.Drums => "Assign keys/pads for the drums.",
        EPlayer.Guitar1 => "Assign keys/pads for the guitar.",
        _ => "Assign keys/pads for the bass."
    };
}
