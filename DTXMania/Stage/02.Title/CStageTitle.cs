﻿using System.Runtime.InteropServices;
using System.Diagnostics;
using DTXUIRenderer;
using FDK;
using SharpDX;
using Rectangle = System.Drawing.Rectangle;
using SlimDXKey = SlimDX.DirectInput.Key;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI;

namespace DTXMania;

internal class CStageTitle : CStage
{		
	// コンストラクタ

	public CStageTitle()
	{
		eStageID = EStage.Title_2;
		bNotActivated = true;
		listChildActivities.Add( actFIfromSetup = new CActFIFOWhite() );
		listChildActivities.Add( actFI = new CActFIFOWhite() );
		listChildActivities.Add( actFO = new CActFIFOWhite() );
	}


	// CStage 実装

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
			ctカーソルフラッシュ用 = new CCounter();
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
			ctカーソルフラッシュ用 = null;
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
		if( !bNotActivated )
		{
			ui = new UIGroup("Title Stage");
			
			txBackground = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\2_background.jpg" ), false );
			txMenu = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\2_menu.png" ), false );
				
			// UI
			var family = new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント);
			ui.AddChild(new UIText(family, 12, CDTXMania.VERSION_DISPLAY));
			
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( !bNotActivated )
		{
			ui.Dispose();

			CDTXMania.tReleaseTexture( ref txBackground );
			CDTXMania.tReleaseTexture( ref txMenu );
			
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( !bNotActivated )
		{
			#region [ 初めての進行描画 ]
			//---------------------
			if( bJustStartedUpdate )
			{
				if( CDTXMania.rPreviousStage == CDTXMania.stageStartup )
				{
					actFIfromSetup.tStartFadeIn();
					ePhaseID = EPhase.タイトル_起動画面からのフェードイン;
				}
				else
				{
					actFI.tStartFadeIn();
					ePhaseID = EPhase.Common_FadeIn;
				}
				ctカーソルフラッシュ用.tStart( 0, 700, 5, CDTXMania.Timer );
				ctカーソルフラッシュ用.nCurrentValue = 100;
				bJustStartedUpdate = false;
			}
			//---------------------
			#endregion

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
			ctカーソルフラッシュ用.tUpdateLoop();
			//---------------------
			#endregion

			// キー入力

			if( ePhaseID == EPhase.Common_DefaultState)
			{
				if( CDTXMania.InputManager.Keyboard.bKeyPressed( (int) SlimDXKey.Escape ) )
					return (int) E戻り値.EXIT;

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
					if ( ( nCurrentCursorPosition == (int) E戻り値.GAMESTART - 1 ) && CDTXMania.Skin.soundGameStart.b読み込み成功 )
					{
						CDTXMania.Skin.soundGameStart.tPlay();
					}
					else
					{
						CDTXMania.Skin.soundDecide.tPlay();
					}
					if( nCurrentCursorPosition == (int)E戻り値.EXIT - 1 )
					{
						return (int)E戻り値.EXIT;
					}
					actFO.tStartFadeOut();
					ePhaseID = EPhase.Common_FadeOut;
				}
			}

			// DrawBackground

			if( txBackground != null )
				txBackground.tDraw2D( CDTXMania.app.Device, 0, 0 );
				
			if( txMenu != null )
			{
				int x = MENU_X;
				int y = MENU_Y + ( nCurrentCursorPosition * MENU_H );
				if( ct上移動用.bInProgress )
				{
					y += (int) ( (double)MENU_H / 2 * ( Math.Cos( Math.PI * ( ( (double) ct上移動用.nCurrentValue ) / 100.0 ) ) + 1.0 ) );
				}
				else if( ct下移動用.bInProgress )
				{
					y -= (int) ( (double)MENU_H / 2 * ( Math.Cos( Math.PI * ( ( (double) ct下移動用.nCurrentValue ) / 100.0 ) ) + 1.0 ) );
				}
				if( ctカーソルフラッシュ用.nCurrentValue <= 100 )
				{
					float nMag = (float) ( 1.0 + ( ( ( (double) ctカーソルフラッシュ用.nCurrentValue ) / 100.0 ) * 0.5 ) );
					txMenu.vcScaleRatio.X = nMag;
					txMenu.vcScaleRatio.Y = nMag;
					txMenu.nTransparency = (int) ( 255.0 * ( 1.0 - ( ( (double) ctカーソルフラッシュ用.nCurrentValue ) / 100.0 ) ) );
					int x_magnified = x + ( (int) ( ( MENU_W * ( 1.0 - nMag ) ) / 2.0 ) );
					int y_magnified = y + ( (int) ( ( MENU_H * ( 1.0 - nMag ) ) / 2.0 ) );
					txMenu.tDraw2D( CDTXMania.app.Device, x_magnified, y_magnified, new Rectangle( 0, MENU_H * 5, MENU_W, MENU_H ) );
				}
				txMenu.vcScaleRatio.X = 1f;
				txMenu.vcScaleRatio.Y = 1f;
				txMenu.nTransparency = 0xff;
				txMenu.tDraw2D( CDTXMania.app.Device, x, y, new Rectangle( 0, MENU_H * 4, MENU_W, MENU_H ) );
			}
			if( txMenu != null )
			{
				txMenu.tDraw2D( CDTXMania.app.Device, MENU_X, MENU_Y, new Rectangle( 0, 0, MENU_W, MENU_H ) );
				txMenu.tDraw2D( CDTXMania.app.Device, MENU_X, MENU_Y + MENU_H, new Rectangle( 0, MENU_H * 2, MENU_W, MENU_H * 2 ) );
			}
				
			ui.Draw(Matrix.Identity);
				
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
					if( actFO.OnUpdateAndDraw() == 0 )
					{
						break;
					}
					ePhaseID = EPhase.Common_EndStatus;
					switch ( nCurrentCursorPosition )
					{
						case (int)E戻り値.GAMESTART - 1:
							return (int)E戻り値.GAMESTART;

						case (int) E戻り値.CONFIG - 1:
							return (int) E戻り値.CONFIG;

						case (int)E戻り値.EXIT - 1:
							return (int) E戻り値.EXIT;
						//return ( this.n現在のカーソル行 + 1 );
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
		}
		return 0;
	}
	public enum E戻り値
	{
		継続 = 0,
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
	private CCounter ctカーソルフラッシュ用;
	private STキー反復用カウンタ ctキー反復用;
	private CCounter ct下移動用;
	private CCounter ct上移動用;
	private const int MENU_H = 0x27;
	private const int MENU_W = 0xe3;
	private const int MENU_X = 0x1fa;
	private const int MENU_Y = 0x201;
	private int nCurrentCursorPosition;
	private CTexture txMenu;
	private CTexture txBackground;
	
	private void tMoveCursorDown()
	{
		if ( nCurrentCursorPosition != (int) E戻り値.EXIT - 1 )
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
		if ( nCurrentCursorPosition != (int) E戻り値.GAMESTART - 1 )
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