using System.Drawing;
using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActPerfStageFailure : CActivity
{
    // コンストラクタ

    public CActPerfStageFailure()
    {
        bNotActivated = true;
    }


    // メソッド

    public void Start()
    {
        ct進行 = new CCounter(0, 0x3e8, 2, CDTXMania.Timer);
    }


    // CActivity 実装

    public override void OnActivate()
    {
        sd効果音 = null;
        b効果音再生済み = false;
        ct進行 = new CCounter();
        base.OnActivate();
    }
    public override void OnDeactivate()
    {
        ct進行 = null;
        if (sd効果音 != null)
        {
            CDTXMania.SoundManager.tDiscard(sd効果音);
            sd効果音 = null;
        }
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if (!bNotActivated)
        {
            txStageFailed = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_stage_failed.jpg"));
            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated)
        {
            CDTXMania.tReleaseTexture(ref txStageFailed);
            base.OnManagedReleaseResources();
        }
    }
    public override int OnUpdateAndDraw()
    {
        if (bNotActivated)
        {
            return 0;
        }
        if ((ct進行 == null) || ct進行.bStopped)
        {
            return 0;
        }
        ct進行.tUpdate();
        if (ct進行.nCurrentValue < 100)
        {
            int x = (int)(640.0 * Math.Cos((Math.PI / 2 * ct進行.nCurrentValue) / 100.0));
            if ((x != 1280) && (txStageFailed != null))
            {
                txStageFailed.tDraw2D(CDTXMania.app.Device, 0, 0, new Rectangle(x, 0, 640 - x, 720));
                txStageFailed.tDraw2D(CDTXMania.app.Device, 640 + x, 0, new Rectangle(640, 0, 640 - x, 720));
            }
        }
        else
        {
            if (txStageFailed != null)
            {
                txStageFailed.tDraw2D(CDTXMania.app.Device, 0, 0);
            }
            if (!b効果音再生済み)
            {
                if (((CDTXMania.DTX.SOUND_STAGEFAILED != null) && (CDTXMania.DTX.SOUND_STAGEFAILED.Length > 0)) && File.Exists(CDTXMania.DTX.strFolderName + CDTXMania.DTX.SOUND_STAGEFAILED))
                {
                    try
                    {
                        if (sd効果音 != null)
                        {
                            CDTXMania.SoundManager.tDiscard(sd効果音);
                            sd効果音 = null;
                        }
                        sd効果音 = CDTXMania.SoundManager.tGenerateSound(CDTXMania.DTX.strFolderName + CDTXMania.DTX.SOUND_STAGEFAILED);
                        sd効果音.tStartPlaying();
                    }
                    catch
                    {
                    }
                }
                else
                {
                    CDTXMania.Skin.soundSTAGEFAILED音.tPlay();
                }
                b効果音再生済み = true;
            }
        }
        if (!ct進行.bReachedEndValue)
        {
            return 0;
        }
        return 1;
    }


    // Other

    #region [ private ]
    //-----------------
    private bool b効果音再生済み;
    private CCounter ct進行;
    private CSound sd効果音;
    private CTexture txStageFailed;
    //-----------------
    #endregion
}