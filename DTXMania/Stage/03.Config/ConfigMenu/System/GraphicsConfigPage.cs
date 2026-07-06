using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class GraphicsConfigPage : ConfigPage
{
    public GraphicsConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        CItemToggle avi = new("AVI (ffmpeg)", CDTXMania.ConfigIni.bAVIEnabled,
            "AVIの使用：\n動画(AVI)を再生可能にする場合に\nON にします。AVI の再生には、それ\nなりのマシンパワーが必要とされます。",
            "Turn ON to enable video (AVI) playback.\nThis requires some processing power.");
        avi.BindConfig(
            () => avi.bON = CDTXMania.ConfigIni.bAVIEnabled,
            () => CDTXMania.ConfigIni.bAVIEnabled = avi.bON);
        items.Add(avi);

        CItemList movieMode = new("Movie Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nMovieMode,
            "Movie Mode:\n0 = 非表示\n1 = 全画面\n2 = ウインドウモード\n3 = 全画面&ウインドウ\n演奏中にF5キーで切り替え。",
            "Movie Mode:\n0 = Hide\n1 = Full screen\n2 = Window mode\n3 = Both Full screen and window\nUse F5 to switch during game.",
            ["Off", "Full Screen", "Window Mode", "Both"]);
        movieMode.BindConfig(
            () => movieMode.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nMovieMode,
            () => CDTXMania.ConfigIni.nMovieMode = movieMode.nCurrentlySelectedIndex);
        items.Add(movieMode);

        CItemToggle bga = new("BGA", CDTXMania.ConfigIni.bBGAEnabled,
            "BGAの使用：\n画像(BGA)を表示可能にする場合に\nON にします。BGA の再生には、それ\nなりのマシンパワーが必要とされます。",
            "Turn ON to enable background animation (BGA) playback.\nThis requires some processing power.");
        bga.BindConfig(
            () => bga.bON = CDTXMania.ConfigIni.bBGAEnabled,
            () => CDTXMania.ConfigIni.bBGAEnabled = bga.bON);
        items.Add(bga);

        CItemInteger bgAlpha = new("BG Alpha", 0, 0xff, CDTXMania.ConfigIni.nBackgroundTransparency,
            "背景画像をDTXManiaの\n背景画像と合成する際の\n背景画像の透明度を\n指定します。\n255に近いほど、不透明\nになります。",
            "Degree of transparency for background wallpaper\n\n0=Completely transparent,\n255=No transparency");
        bgAlpha.BindConfig(
            () => bgAlpha.nCurrentValue = CDTXMania.ConfigIni.nBackgroundTransparency,
            () => CDTXMania.ConfigIni.nBackgroundTransparency = bgAlpha.nCurrentValue);
        items.Add(bgAlpha);

        CItemList movieAlpha = new("LaneAlpha", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nMovieAlpha,
            "レーンの透明度を指定します。\n0% が完全不透明で、\n100% が完全透明となります。",
            "Degree of transparency for Movie.\n\n0%=No transparency,\n100%=Completely transparent",
            ["0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%"]);
        movieAlpha.BindConfig(
            () => movieAlpha.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nMovieAlpha,
            () => CDTXMania.ConfigIni.nMovieAlpha = movieAlpha.nCurrentlySelectedIndex);
        items.Add(movieAlpha);

        // Fullscreen / exclusive / vsync are only written when "Apply Changes" runs (they need a
        // coordinated apply), so these bind read-only.
        CItemToggle fullscreen = new("Fullscreen", CDTXMania.ConfigIni.bFullScreenMode,
            "画面モード設定：\n ON で全画面モード、\n OFF でウィンドウモードになります。",
            "Screen mode: ON for fullscreen, OFF for windowed mode.");
        fullscreen.BindConfig(() => fullscreen.bON = CDTXMania.ConfigIni.bFullScreenMode);
        items.Add(fullscreen);

        CItemToggle exclusiveFullscreen = new("Exclusive Fullscreen", CDTXMania.ConfigIni.bFullScreenExclusive,
            "ONにすると排他的フルスクリーン\nOFFにするとボーダーレスフルスクリーンになります。\n低遅延を実現するには排他的フルスクリーンをおすすめします。",
            "Turn ON for exclusive fullscreen\nOFF for borderless fullscreen.\nExclusive fullscreen is recommended for the lowest latency.");
        exclusiveFullscreen.BindConfig(() => exclusiveFullscreen.bON = CDTXMania.ConfigIni.bFullScreenExclusive);
        items.Add(exclusiveFullscreen);

        CItemToggle vsync = new("Vertical Sync", CDTXMania.ConfigIni.bVerticalSyncWait,
            "垂直帰線同期：\n画面の描画をディスプレイの\n垂直帰線中に行なう場合には\nONを指定します。\nONにすると、ガタつきのない\n滑らかな画面描画が実現されます。",
            "Turn ON to wait VSync at every drawing (so FPS becomes 60).\nIf you have enough CPU/GPU power, the scrolling would become smooth.");
        vsync.BindConfig(() => vsync.bON = CDTXMania.ConfigIni.bVerticalSyncWait);
        items.Add(vsync);

        items.Add(new CItemBase("Apply Changes", CItemBase.EPanelType.Normal,
            "グラフィックの変更を適用する",
            "Apply graphics changes")
        {
            action = () =>
            {
                CDTXMania.ConfigIni.bFullScreenMode = fullscreen.bON;
                CDTXMania.ConfigIni.bFullScreenExclusive = exclusiveFullscreen.bON;
                CDTXMania.ConfigIni.bVerticalSyncWait = vsync.bON;

                CDTXMania.ConfigIni.SyncGraphicsSettings(CDTXMania.app.maniaGl.host);

                // resolution/renderScale may have changed, so re-render the visible text
                list.RefreshAllText();
            }
        });

        items.Add(BackItem());
        return items;
    }
}
