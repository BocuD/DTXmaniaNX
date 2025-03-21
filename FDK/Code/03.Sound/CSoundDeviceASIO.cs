using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.BassAsio;
using Un4seen.Bass.AddOn.Mix;

namespace FDK;

/// <summary>
/// 全ASIOデバイスを列挙する静的クラス。
/// BASS_Init()やBASS_ASIO_Init()の状態とは無関係に使用可能。
/// </summary>
public static class CEnumerateAllAsioDevices
{
	public static string[] GetAllASIODevices()
	{
		//Debug.WriteLine( "BassAsio.BASS_ASIO_GetDeviceInfos():" );
		BASS_ASIO_DEVICEINFO[] bassAsioDevInfo = BassAsio.BASS_ASIO_GetDeviceInfos();

		List<string> asioDeviceList = new List<string>();

		if (bassAsioDevInfo.Length == 0)
		{
			asioDeviceList.Add("None");
		}
		else
		{
			for (int i = 0; i < bassAsioDevInfo.Length; i++)
			{
				asioDeviceList.Add(bassAsioDevInfo[i].name);
				//Trace.TraceInformation( "ASIO Device {0}: {1}", i, bassAsioDevInfo[ i ].name );
			}
		}

		return asioDeviceList.ToArray();
	}
}

public class CSoundDeviceASIO : ISoundDevice
{
	// プロパティ

	public ESoundDeviceType e出力デバイス
	{
		get;
		protected set;
	}
	public long n実出力遅延ms
	{
		get;
		protected set;
	}
	public long n実バッファサイズms
	{
		get;
		protected set;
	}
	public int nASIODevice
	{
		get;
		set;
	}

	// CSoundTimer 用に公開しているプロパティ

	public long n経過時間ms
	{
		get;
		protected set;
	}
	public long n経過時間を更新したシステム時刻ms
	{
		get;
		protected set;
	}
	public CTimer tmシステムタイマ
	{
		get;
		protected set;
	}


	// マスターボリュームの制御コードは、WASAPI/ASIOで全く同じ。
	public int nMasterVolume
	{
		get
		{
			float f音量 = 0.0f;
			bool b = Bass.BASS_ChannelGetAttribute(hMixer, BASSAttribute.BASS_ATTRIB_VOL, ref f音量);
			if (!b)
			{
				BASSError be = Bass.BASS_ErrorGetCode();
				Trace.TraceInformation("ASIO Master Volume Get Error: " + be.ToString());
			}
			else
			{
				//Trace.TraceInformation( "ASIO Master Volume Get Success: " + (f音量 * 100) );

			}
			return (int)(f音量 * 100);
		}
		set
		{
			bool b = Bass.BASS_ChannelSetAttribute(hMixer, BASSAttribute.BASS_ATTRIB_VOL, (float)(value / 100.0));
			if (!b)
			{
				BASSError be = Bass.BASS_ErrorGetCode();
				Trace.TraceInformation("ASIO Master Volume Set Error: " + be.ToString());
			}
			else
			{
				// int n = this.nMasterVolume;	
				// Trace.TraceInformation( "ASIO Master Volume Set Success: " + value );
			}
		}
	}

	public string strDefaultSoundDeviceBusType
	{
		get;
		protected set;
	}

	// メソッド

	public CSoundDeviceASIO(long n希望バッファサイズms, int _nASIODevice)
	{
		// 初期化。

		Trace.TraceInformation("BASS (ASIO) の初期化を開始します。");
		e出力デバイス = ESoundDeviceType.Unknown;
		n実出力遅延ms = 0;
		n経過時間ms = 0;
		n経過時間を更新したシステム時刻ms = CTimer.nUnused;
		tmシステムタイマ = new CTimer(CTimer.EType.MultiMedia);
		nASIODevice = _nASIODevice;

		#region [ BASS registration ]
		// BASS.NET ユーザ登録（BASSスプラッシュが非表示になる）。
		BassNet.Registration("dtxmaniaxgk@gmail.com", "2X9182021152222");
		#endregion

		#region [ BASS Version Check ]
		// BASS のバージョンチェック。
		int nBASSVersion = Utils.HighWord(Bass.BASS_GetVersion());
		if (nBASSVersion != Bass.BASSVERSION)
			throw new DllNotFoundException(string.Format("bass.dll のバージョンが異なります({0})。このプログラムはバージョン{1}で動作します。", nBASSVersion, Bass.BASSVERSION));

		int nBASSMixVersion = Utils.HighWord(BassMix.BASS_Mixer_GetVersion());
		if (nBASSMixVersion != BassMix.BASSMIXVERSION)
			throw new DllNotFoundException(string.Format("bassmix.dll のバージョンが異なります({0})。このプログラムはバージョン{1}で動作します。", nBASSMixVersion, BassMix.BASSMIXVERSION));

		int nBASSASIO = Utils.HighWord(BassAsio.BASS_ASIO_GetVersion());
		if (nBASSASIO != BassAsio.BASSASIOVERSION)
			throw new DllNotFoundException(string.Format("bassasio.dll のバージョンが異なります({0})。このプログラムはバージョン{1}で動作します。", nBASSASIO, BassAsio.BASSASIOVERSION));
		#endregion

		// BASS の設定。

		bIsBASSFree = true;
		Debug.Assert(Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0),       // 0:BASSストリームの自動更新を行わない。（BASSWASAPIから行うため）
			string.Format("BASS_SetConfig() に失敗しました。[{0}", Bass.BASS_ErrorGetCode()));


		// BASS の初期化。

		int nデバイス = 0;      // 0:"no device" … BASS からはデバイスへアクセスさせない。アクセスは BASSASIO アドオンから行う。
		int n周波数 = 44100;   // 仮決め。最終的な周波数はデバイス（≠ドライバ）が決める。
		if (!Bass.BASS_Init(nデバイス, n周波数, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
			throw new Exception(string.Format("BASS の初期化に失敗しました。(BASS_Init)[{0}]", Bass.BASS_ErrorGetCode().ToString()));

		//Debug.WriteLine( "BASS_Init()完了。" );
		#region [ デバッグ用: ASIOデバイスのenumerateと、ログ出力 ]
		//			CEnumerateAllAsioDevices.GetAllASIODevices();
		//Debug.WriteLine( "BassAsio.BASS_ASIO_GetDeviceInfo():" );
		//            int a, count = 0;
		//            BASS_ASIO_DEVICEINFO asioDevInfo;
		//            for ( a = 0; ( asioDevInfo = BassAsio.BASS_ASIO_GetDeviceInfo( a ) ) != null; a++ )
		//            {
		//                Trace.TraceInformation( "ASIO Device {0}: {1}, driver={2}", a, asioDevInfo.name, asioDevInfo.driver );
		//                count++; // count it
		//            }
		#endregion

		// BASS ASIO の初期化。
		BASS_ASIO_INFO asioInfo = null;
		if (BassAsio.BASS_ASIO_Init(nASIODevice, BASSASIOInit.BASS_ASIO_THREAD))    // 専用スレッドにて起動
		{
			#region [ ASIO の初期化に成功。]
			//-----------------
			e出力デバイス = ESoundDeviceType.ASIO;
			asioInfo = BassAsio.BASS_ASIO_GetInfo();
			n出力チャンネル数 = asioInfo.outputs;
			db周波数 = BassAsio.BASS_ASIO_GetRate();
			fmtASIOデバイスフォーマット = BassAsio.BASS_ASIO_ChannelGetFormat(false, 0);

			Trace.TraceInformation("BASS を初期化しました。(ASIO, デバイス:\"{0}\", 入力{1}, 出力{2}, {3}Hz, バッファ{4}～{6}sample ({5:0.###}～{7:0.###}ms), デバイスフォーマット:{8})",
				asioInfo.name,
				asioInfo.inputs,
				asioInfo.outputs,
				db周波数.ToString("0.###"),
				asioInfo.bufmin, asioInfo.bufmin * 1000 / db周波数,
				asioInfo.bufmax, asioInfo.bufmax * 1000 / db周波数,
				fmtASIOデバイスフォーマット.ToString()
			);
			bIsBASSFree = false;
			#region [ debug: channel format ]
			//BASS_ASIO_CHANNELINFO chinfo = new BASS_ASIO_CHANNELINFO();
			//int chan = 0;
			//while ( true )
			//{
			//    if ( !BassAsio.BASS_ASIO_ChannelGetInfo( false, chan, chinfo ) )
			//        break;
			//    Debug.WriteLine( "Ch=" + chan + ": " + chinfo.name.ToString() + ", " + chinfo.group.ToString() + ", " + chinfo.format.ToString() );
			//    chan++;
			//}
			#endregion
			//-----------------
			#endregion
		}
		else
		{
			#region [ ASIO の初期化に失敗。]
			//-----------------
			BASSError errcode = Bass.BASS_ErrorGetCode();
			string errmes = errcode.ToString();
			if (errcode == BASSError.BASS_OK)
			{
				errmes = "BASS_OK; The device may be dissconnected";
			}
			Bass.BASS_Free();
			bIsBASSFree = true;
			throw new Exception(string.Format("BASS (ASIO) の初期化に失敗しました。(BASS_ASIO_Init)[{0}]", errmes));
			//-----------------
			#endregion
		}

		strDefaultSoundDeviceBusType = "";      // ASIOは低遅延前提のはずなので、deafult sound device(のバスタイプ)を気に掛けないことにする

		// ASIO 出力チャンネルの初期化。

		tAsioProc = new ASIOPROC(tAsio処理);        // アンマネージに渡す delegate は、フィールドとして保持しておかないとGCでアドレスが変わってしまう。
		if (!BassAsio.BASS_ASIO_ChannelEnable(false, 0, tAsioProc, IntPtr.Zero))       // 出力チャンネル0 の有効化。
		{
			#region [ ASIO 出力チャンネルの初期化に失敗。]
			//-----------------
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
			bIsBASSFree = true;
			throw new Exception(string.Format("Failed BASS_ASIO_ChannelEnable() [{0}]", BassAsio.BASS_ASIO_ErrorGetCode().ToString()));
			//-----------------
			#endregion
		}
		for (int i = 1; i < n出力チャンネル数; i++)        // 出力チャネルを全てチャネル0とグループ化する。
		{                                                       // チャネル1だけを0とグループ化すると、3ch以上の出力をサポートしたカードでの動作がおかしくなる
			if (!BassAsio.BASS_ASIO_ChannelJoin(false, i, 0))
			{
				#region [ 初期化に失敗。]
				//-----------------
				BassAsio.BASS_ASIO_Free();
				Bass.BASS_Free();
				bIsBASSFree = true;
				throw new Exception(string.Format("Failed BASS_ASIO_ChannelJoin({1}) [{0}]", BassAsio.BASS_ASIO_ErrorGetCode().ToString(), i));
				//-----------------
				#endregion
			}
		}
		if (!BassAsio.BASS_ASIO_ChannelSetFormat(false, 0, fmtASIOチャンネルフォーマット))    // 出力チャンネル0のフォーマット
		{
			#region [ ASIO 出力チャンネルの初期化に失敗。]
			//-----------------
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
			bIsBASSFree = true;
			throw new Exception(string.Format("Failed BASS_ASIO_ChannelSetFormat() [{0}]", BassAsio.BASS_ASIO_ErrorGetCode().ToString()));
			//-----------------
			#endregion
		}

		// ASIO 出力と同じフォーマットを持つ BASS ミキサーを作成。
		// 1つのまとめとなるmixer (hMixer) と、そこにつなぐ複数の楽器別mixer (hMixer _forChips)を作成。

		var flag = BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_STREAM_DECODE;   // デコードのみ＝発声しない。ASIO に出力されるだけ。
		if (fmtASIOデバイスフォーマット == BASSASIOFormat.BASS_ASIO_FORMAT_FLOAT)
			flag |= BASSFlag.BASS_SAMPLE_FLOAT;
		hMixer = BassMix.BASS_Mixer_StreamCreate((int)db周波数, n出力チャンネル数, flag);

		if (hMixer == 0)
		{
			BASSError err = Bass.BASS_ErrorGetCode();
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
			bIsBASSFree = true;
			throw new Exception(string.Format("BASSミキサ(mixing)の作成に失敗しました。[{0}]", err));
		}

		////以下は録音用なので、WASAPIのみで使う
		//for (int i = 0; i < (int)CSound.EInstType.Unknown; i++)
		//{
		//	this.hMixer_forChips[i] = BassMix.BASS_Mixer_StreamCreate((int)this.db周波数, this.n出力チャンネル数, flag);
		//	if (this.hMixer_forChips[i] == 0)
		//	{
		//		BASSError errcode = Bass.BASS_ErrorGetCode();
		//		BassAsio.BASS_ASIO_Free();
		//		Bass.BASS_Free();
		//		this.bIsBASSFree = true;
		//		throw new Exception(string.Format("BASSミキサ(楽器[{1}]ごとのmixing)の作成に失敗しました。[{0}]", errcode, i));
		//	}

		//	bool b1 = BassMix.BASS_Mixer_StreamAddChannel(this.hMixer, this.hMixer_forChips[i], BASSFlag.BASS_DEFAULT);
		//	if (!b1)
		//	{
		//		BASSError errcode = Bass.BASS_ErrorGetCode();
		//		BassAsio.BASS_ASIO_Free();
		//		Bass.BASS_Free();
		//		this.bIsBASSFree = true;
		//		throw new Exception(string.Format("個別BASSミキサ({1}}から(mixing)への接続に失敗しました。[{0}]", errcode, i));
		//	};
		//}



		// BASS ミキサーの1秒あたりのバイト数を算出。

		var mixerInfo = Bass.BASS_ChannelGetInfo(hMixer);
		int nサンプルサイズbyte = 0;
		switch (fmtASIOチャンネルフォーマット)
		{
			case BASSASIOFormat.BASS_ASIO_FORMAT_16BIT: nサンプルサイズbyte = 2; break;
			case BASSASIOFormat.BASS_ASIO_FORMAT_24BIT: nサンプルサイズbyte = 3; break;
			case BASSASIOFormat.BASS_ASIO_FORMAT_32BIT: nサンプルサイズbyte = 4; break;
			case BASSASIOFormat.BASS_ASIO_FORMAT_FLOAT: nサンプルサイズbyte = 4; break;
		}
		//long nミキサーの1サンプルあたりのバイト数 = /*mixerInfo.chans*/ 2 * nサンプルサイズbyte;
		long nミキサーの1サンプルあたりのバイト数 = mixerInfo.chans * nサンプルサイズbyte;
		nミキサーの1秒あたりのバイト数 = nミキサーの1サンプルあたりのバイト数 * mixerInfo.freq;


		// 単純に、hMixerの音量をMasterVolumeとして制御しても、
		// ChannelGetData()の内容には反映されない。
		// そのため、もう一段mixerを噛ませて、一段先のmixerからChannelGetData()することで、
		// hMixerの音量制御を反映させる。
		hMixer_DeviceOut = BassMix.BASS_Mixer_StreamCreate(
			(int)db周波数, n出力チャンネル数, flag);
		if (hMixer_DeviceOut == 0)
		{
			BASSError errcode = Bass.BASS_ErrorGetCode();
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
			bIsBASSFree = true;
			throw new Exception(string.Format("BASSミキサ(最終段)の作成に失敗しました。[{0}]", errcode));
		}
		{
			bool b1 = BassMix.BASS_Mixer_StreamAddChannel(hMixer_DeviceOut, hMixer, BASSFlag.BASS_DEFAULT);
			if (!b1)
			{
				BASSError errcode = Bass.BASS_ErrorGetCode();
				BassAsio.BASS_ASIO_Free();
				Bass.BASS_Free();
				bIsBASSFree = true;
				throw new Exception(string.Format("BASSミキサ(最終段とmixing)の接続に失敗しました。[{0}]", errcode));
			};
		}


		// 出力を開始。

		nバッファサイズsample = (int)(n希望バッファサイズms * db周波数 / 1000.0);
		//this.nバッファサイズsample = (int)  nバッファサイズbyte;
		if (!BassAsio.BASS_ASIO_Start(nバッファサイズsample))     // 範囲外の値を指定した場合は自動的にデフォルト値に設定される。
		{
			BASSError err = BassAsio.BASS_ASIO_ErrorGetCode();
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
			bIsBASSFree = true;
			throw new Exception("ASIO デバイス出力開始に失敗しました。" + err.ToString());
		}
		else
		{
			int n遅延sample = BassAsio.BASS_ASIO_GetLatency(false);   // この関数は BASS_ASIO_Start() 後にしか呼び出せない。
			int n希望遅延sample = (int)(n希望バッファサイズms * db周波数 / 1000.0);
			n実バッファサイズms = n実出力遅延ms = (long)(n遅延sample * 1000.0f / db周波数);
			Trace.TraceInformation("ASIO デバイス出力開始：バッファ{0}sample(希望{1}) [{2}ms(希望{3}ms)]", n遅延sample, n希望遅延sample, n実出力遅延ms, n希望バッファサイズms);
		}
	}

	#region [ 録音制御用(WASAPI以外でのみ使用) ]
	public bool tStartRecording()
	{
		return false;
	}
	public bool tStopRecording()
	{
		return false;
	}
	#endregion

	#region [ tサウンドを作成する() ]
	public CSound tサウンドを作成する(string strファイル名)
	{
		return tサウンドを作成する(strファイル名, CSound.EInstType.Unknown);
	}
	public CSound tサウンドを作成する(string strファイル名, CSound.EInstType eInstType)
	{
		var sound = new CSound();
		sound.tASIOサウンドを作成する(strファイル名, hMixer, eInstType);
		return sound;
	}
	public CSound tサウンドを作成する(byte[] byArrWAVファイルイメージ)
	{
		return tサウンドを作成する(byArrWAVファイルイメージ, CSound.EInstType.Unknown);
	}
	public CSound tサウンドを作成する(byte[] byArrWAVファイルイメージ, CSound.EInstType eInstType)
	{
		var sound = new CSound();
		sound.tASIOサウンドを作成する(byArrWAVファイルイメージ, hMixer, eInstType);
		return sound;
	}
	public void tサウンドを作成する(string strファイル名, ref CSound sound, CSound.EInstType eInstType)
	{
		sound.tASIOサウンドを作成する(strファイル名, hMixer, eInstType);
	}
	public void tサウンドを作成する(byte[] byArrWAVファイルイメージ, ref CSound sound, CSound.EInstType eInstType)
	{
		sound.tASIOサウンドを作成する(byArrWAVファイルイメージ, hMixer, eInstType);
	}
	#endregion


	#region [ Dispose-Finallizeパターン実装 ]
	//-----------------
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	protected void Dispose(bool bManagedDispose)
	{
		e出力デバイス = ESoundDeviceType.Unknown;        // まず出力停止する(Dispose中にクラス内にアクセスされることを防ぐ)
		if (hMixer_DeviceOut != 0)
		{
			BassMix.BASS_Mixer_ChannelPause(hMixer_DeviceOut);
			Bass.BASS_StreamFree(hMixer_DeviceOut);
			hMixer_DeviceOut = 0;
		}
		if (hMixer != 0)
		{
			BassMix.BASS_Mixer_ChannelPause(hMixer);
			Bass.BASS_StreamFree(hMixer);
			hMixer = 0;
		}
		if (!bIsBASSFree)
		{
			BassAsio.BASS_ASIO_Free();  // システムタイマより先に呼び出すこと。（tAsio処理() の中でシステムタイマを参照してるため）
			Bass.BASS_Free();
		}

		if (bManagedDispose)
		{
			CCommon.tDispose(tmシステムタイマ);
			tmシステムタイマ = null;
		}
	}
	~CSoundDeviceASIO()
	{
		Dispose(false);
	}
	//-----------------
	#endregion


	protected int hMixer = 0;
	protected int hMixer_DeviceOut = 0;
	//protected int[] hMixer_forChips = new int[(int)CSound.EInstType.Unknown];  //DTX2WAV対応 BGM, SE, Drums...を別々のmixerに入れて、個別に音量変更できるようにする
	protected int n出力チャンネル数 = 0;
	protected double db周波数 = 0.0;
	protected int nバッファサイズsample = 0;
	protected BASSASIOFormat fmtASIOデバイスフォーマット = BASSASIOFormat.BASS_ASIO_FORMAT_UNKNOWN;
	protected BASSASIOFormat fmtASIOチャンネルフォーマット = BASSASIOFormat.BASS_ASIO_FORMAT_16BIT;        // 16bit 固定
	//protected BASSASIOFormat fmtASIOチャンネルフォーマット = BASSASIOFormat.BASS_ASIO_FORMAT_32BIT;// 16bit 固定
	protected ASIOPROC tAsioProc = null;

	protected int tAsio処理(bool input, int channel, IntPtr buffer, int length, IntPtr user)
	{
		if (input) return 0;


		// BASSミキサからの出力データをそのまま ASIO buffer へ丸投げ。

		int num = Bass.BASS_ChannelGetData(hMixer_DeviceOut, buffer, length);      // num = 実際に転送した長さ

		if (num == -1) num = 0;


		// 経過時間を更新。
		// データの転送差分ではなく累積転送バイト数から算出する。

		n経過時間ms = (n累積転送バイト数 * 1000 / nミキサーの1秒あたりのバイト数) - n実出力遅延ms;
		n経過時間を更新したシステム時刻ms = tmシステムタイマ.nSystemTimeMs;


		// 経過時間を更新後に、今回分の累積転送バイト数を反映。

		n累積転送バイト数 += num;
		return num;
	}

	private long nミキサーの1秒あたりのバイト数 = 0;
	private long n累積転送バイト数 = 0;
	private bool bIsBASSFree = true;
}