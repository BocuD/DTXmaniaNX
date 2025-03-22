using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using DTXMania.Core;
using DTXMania.UI;
using DTXUIRenderer;
using SharpDX;
using FDK;

namespace DTXMania;

internal class CActEnumSongs : CActivity
{
    public bool bコマンドでの曲データ取得;


    /// <summary>
    /// Constructor
    /// </summary>
    public CActEnumSongs()
    {
        Init(false);
    }
        
    private void Init(bool _bコマンドでの曲データ取得)
    {
        bNotActivated = true;
        bコマンドでの曲データ取得 = _bコマンドでの曲データ取得;
    }

    // CActivity 実装

    public override void OnActivate()
    {
        if (bActivated)
            return;
        base.OnActivate();
            
        ctNowEnumeratingSongs = new CCounter();	// 0, 1000, 17, CDTXMania.Timer );
        ctNowEnumeratingSongs.tStart(0, 100, 17, CDTXMania.Timer);
    }
    public override void OnDeactivate()
    {
        if (bNotActivated)
            return;
        base.OnDeactivate();
        ctNowEnumeratingSongs = null;
    }
    public override void OnManagedCreateResources()
    {
        if (bNotActivated)
            return;
        
        string[] strMessage = 
        [
            "曲データの一覧を\n取得しています。\nしばらくお待ちください。",
            "Now enumerating songs.\nPlease wait..."
        ];

        var parent = CDTXMania.rCurrentStage.ui;
            
        var fontFamily = new FontFamily(CDTXMania.ConfigIni.songListFont);

        //create ui
        enumMessage = parent.AddChild(new UIGroup("EnumSongs"));
        enumMessage.size = new Vector2(1280, 720);
        UIText message = enumMessage.AddChild(new UIText(fontFamily, 12, CDTXMania.isJapanese ? strMessage[0] : strMessage[1]));
        message.position = new Vector3(0, 25, 0);
        message.anchor = new Vector2(0.0f, 0.0f);
            
        text = enumMessage.AddChild(new UIText(fontFamily, 12, "Step 0/0"));
        text.position = new Vector3(0, 60, 0);
        text.anchor = new Vector2(0.0f, 0.0f);
        
        base.OnManagedCreateResources();
    }
    public override void OnManagedReleaseResources()
    {
        if (bNotActivated)
            return;
        
        base.OnManagedReleaseResources();
    }

    private UIGroup enumMessage;
    private UIText text;
    
    public override int OnUpdateAndDraw()
    {
        if (bNotActivated)
        {
            return 0;
        }
        ctNowEnumeratingSongs.tUpdateLoop();
            
        //update ui
        CEnumSongs.SongEnumProgress? status = CDTXMania.EnumSongs.EnumProgress;
        if (status != null)
        {
            int stepCount = Enum.GetValues(typeof(CEnumSongs.SongEnumProgress)).Length;

            switch (status)
            {
                case CEnumSongs.SongEnumProgress.ReadSongData:
                    text.SetText($"Step {(int)status + 1} / {stepCount}\n{status}: " +
                                 $"{CDTXMania.EnumSongs.SongManager.ProcessSongDataProgress} / {CDTXMania.EnumSongs.SongManager.ProcessSongDataTotal}\n" +
                                 $"{CDTXMania.EnumSongs.SongManager.ProcessSongDataPath}");
                    break;
                    
                default:
                    text.SetText($"Step {(int)status + 1} / {stepCount}\n{status}");
                    break;
            }
                
            enumMessage.isVisible = true;
        }
        else
        {
            enumMessage.isVisible = false;
        }

        double fade = Math.Sin(2 * Math.PI * ctNowEnumeratingSongs.nCurrentValue * 2 / 100.0);
            
        //convert to 0-1 range
        text.Texture.transparency = (float)(fade + 1) / 2;
        
        return 0;
    }
        
    private CCounter ctNowEnumeratingSongs;
}