using System.Diagnostics;
using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActSelectPresound : CActivity
{
	// メソッド

	public CActSelectPresound()
	{
		bActivated = false;
	}

	public void tStopSound()
	{
		if (sound != null)
		{
			sound.tStopPlayback();
			CDTXMania.SoundManager.tDiscard(sound);
			sound = null;
		}
	}

	public void tSelectionChanged(CScore chart)
	{
		if (chart != null &&
		    (!(chart.FileInformation.AbsoluteFolderPath + chart.SongInformation.Presound).Equals(strCurrentlyPlayingAudioPath) ||
		     sound == null || !sound.bIsPlaying))
		{
			tStopSound();
			tStartFadeInBgm();
			selectedChart = chart;
			
			if (chart.SongInformation.Presound != null && chart.SongInformation.Presound.Length > 0)
			{
				ctWaitForPlayback = new CCounter(0, CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs, 1, CDTXMania.Timer);
			}
		}
	}


	// CActivity 実装

	public override void OnActivate()
	{
		sound = null;
		strCurrentlyPlayingAudioPath = "";
		ctWaitForPlayback = null;
		ctBgmFadeOut = null;
		ctBgmFadeIn = null;
		base.OnActivate();
	}

	public override void OnDeactivate()
	{
		tStopSound();
		ctWaitForPlayback = null;
		ctBgmFadeIn = null;
		ctBgmFadeOut = null;
		base.OnDeactivate();
	}

	public override int OnUpdateAndDraw()
	{
		if (bActivated)
		{
			if (ctBgmFadeIn != null && ctBgmFadeIn.bInProgress)
			{
				ctBgmFadeIn.tUpdate();
				CDTXMania.Skin.bgmSongSelectScreen.nCurrentSoundVolume = ctBgmFadeIn.nCurrentValue;
				if (ctBgmFadeIn.bReachedEndValue)
				{
					ctBgmFadeIn.tStop();
				}
			}

			if (ctBgmFadeOut != null && ctBgmFadeOut.bInProgress)
			{
				ctBgmFadeOut.tUpdate();
				CDTXMania.Skin.bgmSongSelectScreen.nCurrentSoundVolume = 100 - ctBgmFadeOut.nCurrentValue;
				if (ctBgmFadeOut.bReachedEndValue)
				{
					ctBgmFadeOut.tStop();
				}
			}

			t進行処理_プレビューサウンド();
		}

		return 0;
	}


	// Other

	#region [ private ]

	private CCounter ctBgmFadeOut;
	private CCounter ctBgmFadeIn;
	private CCounter ctWaitForPlayback;
	private CSound sound;
	
	private string strCurrentlyPlayingAudioPath;

	private void tStartFadeOutBgm()
	{
		if (ctBgmFadeIn != null)
		{
			ctBgmFadeIn.tStop();
		}

		ctBgmFadeOut = new CCounter(0, 100, 10, CDTXMania.Timer)
		{
			nCurrentValue = 100 - CDTXMania.Skin.bgmSongSelectScreen.nCurrentSoundVolume
		};
	}

	private void tStartFadeInBgm()
	{
		if (ctBgmFadeOut != null)
		{
			ctBgmFadeOut.tStop();
		}

		ctBgmFadeIn = new CCounter(0, 100, 20, CDTXMania.Timer)
		{
			nCurrentValue = CDTXMania.Skin.bgmSongSelectScreen.nCurrentSoundVolume
		};
	}

	private CScore? selectedChart;
	private void tLoadPreviewSound()
	{
		if (selectedChart != null && !string.IsNullOrEmpty(selectedChart.SongInformation.Presound))
		{
			string strPreviewFilename = selectedChart.FileInformation.AbsoluteFolderPath +
			                            selectedChart.SongInformation.Presound;
			try
			{
				sound = CDTXMania.SoundManager.tGenerateSound(strPreviewFilename);
				sound.nVolume = 80; // CDTXMania.ConfigIni.n自動再生音量;			// #25217 changed preview volume from AutoVolume
				sound.tStartPlaying(true);
				strCurrentlyPlayingAudioPath = strPreviewFilename;
				tStartFadeOutBgm();
				Trace.TraceInformation("プレビューサウンドを生成しました。({0})", strPreviewFilename);
			}
			catch
			{
				Trace.TraceError("プレビューサウンドの生成に失敗しました。({0})", strPreviewFilename);
				if (sound != null)
				{
					sound.Dispose();
				}

				sound = null;
			}
		}
	}

	private void t進行処理_プレビューサウンド()
	{
		if (ctWaitForPlayback != null && !ctWaitForPlayback.bStopped)
		{
			ctWaitForPlayback.tUpdate();
			if (ctWaitForPlayback.bReachedEndValue)
			{
				ctWaitForPlayback.tStop();
				if (!CDTXMania.stageSongSelection.bScrolling)
				{
					tLoadPreviewSound();
				}
			}
		}
	}

	//-----------------

	#endregion
}