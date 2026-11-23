using DTXMania.Core;
using DTXMania.UI.Config;

namespace DTXMania;

internal abstract class InstrumentOptionsPage : PlayerConfigPage
{
    protected InstrumentOptionsPage(ConfigList list, EPlayer player) : base(list, player)
    {
    }

    public static InstrumentOptionsPage For(ConfigList list, EPlayer player) => player == EPlayer.Drums
        ? new DrumsOptionsPage(list)
        : new GuitarOptionsPage(list, player);
}
