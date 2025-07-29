using System.Runtime.InteropServices;
using System.Diagnostics;
using FDK;
using SharpDX;
using Rectangle = System.Drawing.Rectangle;
using SlimDXKey = SlimDX.DirectInput.Key;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Video;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using SharpDX.Direct3D9;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania;

internal class CStageTitle : CStage
{		
	// コンストラクタ

	private FFmpegVideoPlayer videoPlayerTest;
	private bool videoLoaded = false;

	public CStageTitle()
	{
		eStageID = EStage.Title_2;
		bActivated = false;
		listChildActivities.Add(actFIfromSetup = new CActFIFOWhite());
		listChildActivities.Add(actFI = new CActFIFOWhite());
		listChildActivities.Add(actFO = new CActFIFOWhite());

		videoPlayerTest = new SoftwareVideoPlayer();
		videoLoaded = videoPlayerTest.Open(CSkin.Path(@"Graphics\2_background.mp4"));
	}


	// CStage 実装

	public override void InitializeBaseUI()
	{
		
	}

	public override void InitializeDefaultUI()
	{
		var family = new FontFamily(CDTXMania.ConfigIni.songListFont);
		ui.AddChild(new UIText(family, 12, CDTXMania.VERSION_DISPLAY));

		DTXTexture bgTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\2_background.png"));
		UIImage bg = ui.AddChild(new UIImage(bgTex));
		bg.renderOrder = -99;
		bg.position = Vector3.Zero;

		if (videoLoaded)
		{
			VideoPlayer videoPlayer = ui.AddChild(new VideoPlayer(videoPlayerTest));
			videoPlayer.renderOrder = -100;
		}
	}

	public override void OnActivate()
	{
		Trace.TraceInformation( "タイトルステージを活性化します。" );
		Trace.Indent();
		try
		{
			for( int i = 0; i < 4; i++ )
			{
				ctキー反復用[ i ] = new CCounter( 0, 0, 0, CDTXMania.Timer );
			}
			ct上移動用 = new CCounter();
			ct下移動用 = new CCounter();
			ctCursorFlash = new CCounter();
			
			dynamicStringSources["Version"] = new DynamicStringSource(() => CDTXMania.VERSION_DISPLAY);

			base.OnActivate();
		}
		finally
		{
			Trace.TraceInformation( "タイトルステージの活性化を完了しました。" );
			Trace.Unindent();
		}
	}
	public override void OnDeactivate()
	{
		Trace.TraceInformation( "タイトルステージを非活性化します。" );
		Trace.Indent();
		try
		{
			for( int i = 0; i < 4; i++ )
			{
				ctキー反復用[ i ] = null;
			}
			ct上移動用 = null;
			ct下移動用 = null;
			ctCursorFlash = null;
		}
		finally
		{
			Trace.TraceInformation( "タイトルステージの非活性化を完了しました。" );
			Trace.Unindent();
		}
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			txMenu = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\2_menu.png" ), false );
			
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txMenu );
			
			base.OnManagedReleaseResources();
		}
	}

	public override void FirstUpdate()
	{
		if (CDTXMania.StageManager.rPreviousStage == CDTXMania.StageManager.stageStartup )
		{
			actFIfromSetup.tStartFadeIn();
			ePhaseID = EPhase.タイトル_起動画面からのフェードイン;
		}
		else
		{
			actFI.tStartFadeIn();
			ePhaseID = EPhase.Common_FadeIn;
		}
		ctCursorFlash.tStart( 0, 700, 5, CDTXMania.Timer );
		ctCursorFlash.nCurrentValue = 100;
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;
		
		base.OnUpdateAndDraw();

		Matrix scaleMatrix = Matrix.Scaling(CDTXMania.renderScale);
		
		// 進行

		#region [ カーソル上移動 ]
		//---------------------
		if( ct上移動用.bInProgress )
		{
			ct上移動用.tUpdate();
			if( ct上移動用.bReachedEndValue )
			{
				ct上移動用.tStop();
			}
		}
		//---------------------
		#endregion
		#region [ カーソル下移動 ]
		//---------------------
		if( ct下移動用.bInProgress )
		{
			ct下移動用.tUpdate();
			if( ct下移動用.bReachedEndValue )
			{
				ct下移動用.tStop();
			}
		}
		//---------------------
		#endregion
		#region [ カーソルフラッシュ ]
		//---------------------
		ctCursorFlash.tUpdateLoop();
		//---------------------
		#endregion

		// キー入力

		if( ePhaseID == EPhase.Common_DefaultState)
		{
			if( CDTXMania.InputManager.Keyboard.bKeyPressed( (int) SlimDXKey.Escape ) )
				return (int) EReturnResult.EXIT;

			ctキー反復用.Up.tRepeatKey( CDTXMania.InputManager.Keyboard.bKeyPressing( (int)SlimDXKey.UpArrow ), new CCounter.DGキー処理( tMoveCursorUp ) );
			ctキー反復用.R.tRepeatKey( CDTXMania.Pad.bPressingGB( EPad.HH ), new CCounter.DGキー処理( tMoveCursorUp ) );
			//Change to HT
			if( CDTXMania.Pad.bPressed( EInstrumentPart.DRUMS, EPad.HT ) )
				tMoveCursorUp();

			ctキー反復用.Down.tRepeatKey( CDTXMania.InputManager.Keyboard.bKeyPressing( (int)SlimDXKey.DownArrow ), new CCounter.DGキー処理( tMoveCursorDown ) );
			ctキー反復用.B.tRepeatKey( CDTXMania.Pad.bPressingGB( EPad.SD ), new CCounter.DGキー処理( tMoveCursorDown ) );
			//Change to LT
			if ( CDTXMania.Pad.bPressed( EInstrumentPart.DRUMS, EPad.LT ) )
				tMoveCursorDown();

			if (CDTXMania.Input.ActionDecide())
			{
				if ( nCurrentCursorPosition == (int) EReturnResult.GAMESTART - 1 && CDTXMania.Skin.soundGameStart.b読み込み成功 )
				{
					CDTXMania.Skin.soundGameStart.tPlay();
				}
				else
				{
					CDTXMania.Skin.soundDecide.tPlay();
				}
				if( nCurrentCursorPosition == (int)EReturnResult.EXIT - 1 )
				{
					return (int)EReturnResult.EXIT;
				}
				GitaDoraTransition.Close();
				//actFO.tStartFadeOut();
				ePhaseID = EPhase.Common_FadeOut;
			}
		}
		
		if( txMenu != null )
		{
			int x = MENU_X;
			int y = MENU_Y + nCurrentCursorPosition * MENU_H;
			if( ct上移動用.bInProgress )
			{
				y += (int) ( (double)MENU_H / 2 * ( Math.Cos( Math.PI * ( ct上移動用.nCurrentValue / 100.0 ) ) + 1.0 ) );
			}
			else if( ct下移動用.bInProgress )
			{
				y -= (int) ( (double)MENU_H / 2 * ( Math.Cos( Math.PI * ( ct下移動用.nCurrentValue / 100.0 ) ) + 1.0 ) );
			}
			if (ctCursorFlash.nCurrentValue <= 100)
			{
				float nMag = (float)(1.0 + ctCursorFlash.nCurrentValue / 100.0 * 0.5);

				// Apply scale and center pivot offset
				Matrix scaleEffectMatrix = Matrix.Scaling(nMag, nMag, 1.0f);
				Matrix translateMatrix = Matrix.Translation(
					x + (MENU_W * (1.0f - nMag) / 2.0f),
					y + (MENU_H * (1.0f - nMag) / 2.0f),
					0.0f
				);
				Matrix finalMatrix = scaleEffectMatrix * translateMatrix * scaleMatrix;

				txMenu.nTransparency = (int) ( 255.0 * ( 1.0 - ( ( (double) ctCursorFlash.nCurrentValue ) / 100.0 ) ) );
				txMenu.tDraw2DMatrix(CDTXMania.app.Device, finalMatrix, new Vector2(MENU_W, MENU_H), new RectangleF(0, MENU_H * 5, MENU_W, MENU_H));
			}
			txMenu.vcScaleRatio.X = 1f;
			txMenu.vcScaleRatio.Y = 1f;
			txMenu.nTransparency = 0xff;
			
			Matrix mat2 = Matrix.Translation(x, y, 0f) * scaleMatrix;
			txMenu.tDraw2DMatrix( CDTXMania.app.Device, mat2, new Vector2(MENU_W, MENU_H), new RectangleF( 0, MENU_H * 4, MENU_W, MENU_H ) );
		}
		if( txMenu != null )
		{
			Matrix mat = Matrix.Translation(MENU_X, MENU_Y, 0) * scaleMatrix;
			Matrix mat2 = Matrix.Translation(MENU_X, MENU_Y + MENU_H, 0) * scaleMatrix;
			txMenu.tDraw2DMatrix( CDTXMania.app.Device, mat, new Vector2(MENU_W, MENU_H), new RectangleF( 0, 0, MENU_W, MENU_H ));
			txMenu.tDraw2DMatrix( CDTXMania.app.Device, mat2, new Vector2(MENU_W, MENU_H * 2), new RectangleF( 0, MENU_H * 2, MENU_W, MENU_H * 2 ) );
		}
				
		EPhase ePhaseId = ePhaseID;
		switch( ePhaseId )
		{
			case EPhase.Common_FadeIn:
				if( actFI.OnUpdateAndDraw() != 0 )
				{
					CDTXMania.Skin.soundTitle.tPlay();
					ePhaseID = EPhase.Common_DefaultState;
				}
				break;

			case EPhase.Common_FadeOut:
				if (GitaDoraTransition.isAnimating) break;
				
				ePhaseID = EPhase.Common_EndStatus;
				
				switch ( nCurrentCursorPosition )
				{ 
					case (int)EReturnResult.GAMESTART - 1:
						return (int)EReturnResult.GAMESTART;

					case (int) EReturnResult.CONFIG - 1:
						return (int) EReturnResult.CONFIG;

					case (int)EReturnResult.EXIT - 1:
						return (int) EReturnResult.EXIT;
				}
				break;

			case EPhase.タイトル_起動画面からのフェードイン:
				if( actFIfromSetup.OnUpdateAndDraw() != 0 )
				{
					CDTXMania.Skin.soundTitle.tPlay();
					ePhaseID = EPhase.Common_DefaultState;
				}
				break;
		}
		return 0;
	}
	public enum EReturnResult
	{
		CONTINUE = 0,
		GAMESTART,
		CONFIG,
		EXIT
	}


	// Other

	#region [ private ]
	//-----------------
	[StructLayout( LayoutKind.Sequential )]
	private struct STキー反復用カウンタ
	{
		public CCounter Up;
		public CCounter Down;
		public CCounter R;
		public CCounter B;
		public CCounter this[ int index ]
		{
			get
			{
				switch( index )
				{
					case 0:
						return Up;

					case 1:
						return Down;

					case 2:
						return R;

					case 3:
						return B;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case 0:
						Up = value;
						return;

					case 1:
						Down = value;
						return;

					case 2:
						R = value;
						return;

					case 3:
						B = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	private CActFIFOWhite actFI;
	private CActFIFOWhite actFIfromSetup;
	private CActFIFOWhite actFO;
	private CCounter ctCursorFlash;
	private STキー反復用カウンタ ctキー反復用;
	private CCounter ct下移動用;
	private CCounter ct上移動用;
	private const int MENU_H = 0x27;
	private const int MENU_W = 0xe3;
	private const int MENU_X = 0x1fa;
	private const int MENU_Y = 0x201;
	private int nCurrentCursorPosition;
	private CTexture txMenu;
	
	private void tMoveCursorDown()
	{
		if ( nCurrentCursorPosition != (int) EReturnResult.EXIT - 1 )
		{
			CDTXMania.Skin.soundCursorMovement.tPlay();
			nCurrentCursorPosition++;
			ct下移動用.tStart( 0, 100, 1, CDTXMania.Timer );
			if( ct上移動用.bInProgress )
			{
				ct下移動用.nCurrentValue = 100 - ct上移動用.nCurrentValue;
				ct上移動用.tStop();
			}
		}
	}
	private void tMoveCursorUp()
	{
		if ( nCurrentCursorPosition != (int) EReturnResult.GAMESTART - 1 )
		{
			CDTXMania.Skin.soundCursorMovement.tPlay();
			nCurrentCursorPosition--;
			ct上移動用.tStart( 0, 100, 1, CDTXMania.Timer );
			if( ct下移動用.bInProgress )
			{
				ct上移動用.nCurrentValue = 100 - ct下移動用.nCurrentValue;
				ct下移動用.tStop();
			}
		}
	}
	//-----------------
	#endregion
}