using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal partial class CActConfigList
{
    private void tSetupItemList_Graphics()
    {
        tRecordToConfigIni();
        listItems.Clear();

        CItemToggle iSystemAVI = new("AVI", CDTXMania.ConfigIni.bAVIEnabled,
            "AVIの使用：\n動画(AVI)を再生可能にする場合に\nON にします。AVI の再生には、それ\nなりのマシンパワーが必要とされます。",
            "Turn ON to enable video (AVI) playback.\nThis requires some processing power.");
        iSystemAVI.BindConfig(
            () => iSystemAVI.bON = CDTXMania.ConfigIni.bAVIEnabled,
            () => CDTXMania.ConfigIni.bAVIEnabled = iSystemAVI.bON);
        listItems.Add(iSystemAVI);
        
        CItemList iSystemMovieMode = new("Movie Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nMovieMode,
            "Movie Mode:\n0 = 非表示\n1 = 全画面\n2 = ウインドウモード\n3 = 全画面&ウインドウ\n演奏中にF5キーで切り替え。",
            "Movie Mode:\n0 = Hide\n1 = Full screen\n2 = Window mode\n3 = Both Full screen and window\nUse F5 to switch during game.",
            ["Off", "Full Screen", "Window Mode", "Both"]);
        iSystemMovieMode.BindConfig(
            () => iSystemMovieMode.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nMovieMode,
            () => CDTXMania.ConfigIni.nMovieMode = iSystemMovieMode.nCurrentlySelectedIndex);
        listItems.Add(iSystemMovieMode);

        CItemToggle iSystemBGA = new("BGA", CDTXMania.ConfigIni.bBGAEnabled,
            "BGAの使用：\n画像(BGA)を表示可能にする場合に\nON にします。BGA の再生には、それ\nなりのマシンパワーが必要とされます。",
            "Turn ON to enable background animation (BGA) playback.\nThis requires some processing power.");
        iSystemBGA.BindConfig(
            () => iSystemBGA.bON = CDTXMania.ConfigIni.bBGAEnabled,
            () => CDTXMania.ConfigIni.bBGAEnabled = iSystemBGA.bON);
        listItems.Add(iSystemBGA);
        
        CItemInteger iSystemBGAlpha = new("BG Alpha", 0, 0xff, CDTXMania.ConfigIni.nBackgroundTransparency,
            "背景画像をDTXManiaの\n"+
            "背景画像と合成する際の\n"+
            "背景画像の透明度を\n"+
            "指定します。\n"+
            "255に近いほど、不透明\n"+
            "になります。",
            "Degree of transparency for background wallpaper\n\n0=Completely transparent,\n255=No transparency");
        iSystemBGAlpha.BindConfig(
            () => iSystemBGAlpha.nCurrentValue = CDTXMania.ConfigIni.nBackgroundTransparency,
            () => CDTXMania.ConfigIni.nBackgroundTransparency = iSystemBGAlpha.nCurrentValue);
        listItems.Add(iSystemBGAlpha);
        
        CItemList iSystemMovieAlpha = new("LaneAlpha", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nMovieAlpha,
            "レーンの透明度を指定します。\n0% が完全不透明で、\n100% が完全透明となります。",
            "Degree of transparency for Movie.\n\n0%=No transparency,\n100%=Completely transparent",
            ["0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%"]);
        iSystemMovieAlpha.BindConfig(
            () => iSystemMovieAlpha.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nMovieAlpha,
            () => CDTXMania.ConfigIni.nMovieAlpha = iSystemMovieAlpha.nCurrentlySelectedIndex);
        listItems.Add(iSystemMovieAlpha);
        
        CItemToggle iSystemFullscreen = new("Fullscreen", CDTXMania.ConfigIni.bFullScreenMode,
            "画面モード設定：\n ON で全画面モード、\n OFF でウィンドウモードになります。",
            "Screen mode: ON for fullscreen, OFF for windowed mode.");
        iSystemFullscreen.BindConfig(
            () => iSystemFullscreen.bON = CDTXMania.ConfigIni.bFullScreenMode);
        listItems.Add(iSystemFullscreen);

        CItemToggle iSystemExclusiveFullscreen = new("Exclusive Fullscreen", CDTXMania.ConfigIni.bFullScreenMode,
            "ONにすると排他的フルスクリーン\nOFFにするとボーダーレスフルスクリーンになります。\n低遅延を実現するには排他的フルスクリーンをおすすめします。",
            "Turn ON for exclusive fullscreen\nOFF for borderless fullscreen.\n" +
            "Exclusive fullscreen is recommended for the lowest latency.");
        iSystemExclusiveFullscreen.BindConfig(
            () => iSystemExclusiveFullscreen.bON = CDTXMania.ConfigIni.bFullScreenExclusive);
        listItems.Add(iSystemExclusiveFullscreen);
        
        CItemToggle iSystemVSyncWait = new("Vertical Sync", CDTXMania.ConfigIni.bVerticalSyncWait,
            "垂直帰線同期：\n" +
            "画面の描画をディスプレイの\n" +
            "垂直帰線中に行なう場合には\n" +
            "ONを指定します。\n" + 
            "ONにすると、ガタつきのない\n" +
            "滑らかな画面描画が実現されます。",
            "Turn ON to wait VSync (Vertical Synchronizing signal) at every drawing (so FPS becomes 60)\nIf you have enough CPU/GPU power, the scrolling would become smooth.");
        iSystemVSyncWait.BindConfig(
            () => iSystemVSyncWait.bON = CDTXMania.ConfigIni.bVerticalSyncWait);
        listItems.Add(iSystemVSyncWait);
        
        CItemBase iApplyChanges = new("Apply Changes", CItemBase.EPanelType.Normal,
            "グラフィックの変更を適用する",
            "Apply graphics changes")
        {
            action = () =>
            {
                CDTXMania.ConfigIni.bFullScreenMode = iSystemFullscreen.bON;
                CDTXMania.ConfigIni.bFullScreenExclusive = iSystemExclusiveFullscreen.bON;
                CDTXMania.ConfigIni.bVerticalSyncWait = iSystemVSyncWait.bON;
                
                CDTXMania.ConfigIni.SyncGraphicsSettings(CDTXMania.app.maniaGl.host);
                
                //force redrawing all text at the new resolution
                for (int index = 0; index < listMenu.Length; index++)
                {
                    listMenu[index].txItemName?.Dispose();
                    listMenu[index].txItemName = null;
                    
                    listMenu[index].txItemParam?.Dispose();
                    listMenu[index].txItemParam = null;
                }
            }
        };
        listItems.Add(iApplyChanges);
       
        tAddReturnToMenuItem(tSetupItemList_System);

        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemGraphics;
    }
}