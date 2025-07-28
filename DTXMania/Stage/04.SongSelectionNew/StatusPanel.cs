using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania;

public class StatusPanel : UIGroup
{
	public StatusPanel() : base("StatusPanel")
	{
		drums = AddChild(new StatusPane(EInstrumentPart.DRUMS));
		drums.position = new Vector3(430, 720, 0);

		guitar = AddChild(new StatusPane(EInstrumentPart.GUITAR));
		guitar.position = new Vector3(200, 720, 0);

		bass = AddChild(new StatusPane(EInstrumentPart.BASS));
		bass.position = new Vector3(430, 720, 0);

		instruments = [drums, guitar, bass];
		
		CommandHistory = new CStageSongSelection.CCommandHistory();
	}

	public void SelectionChanged(SongNode song, CScore chart)
	{
		foreach (StatusPane pane in instruments)
		{
			pane.song = song;
		}
	}

	private StatusPane drums;
	private StatusPane guitar;
	private StatusPane bass;

	private StatusPane[] instruments;

	public override void Draw(Matrix parentMatrix)
	{
		drums.isVisible = CDTXMania.ConfigIni.bDrumsEnabled;
		guitar.isVisible = !CDTXMania.ConfigIni.bDrumsEnabled;
		bass.isVisible = !CDTXMania.ConfigIni.bDrumsEnabled;

		base.Draw(parentMatrix);
	}

	private CStageSongSelection.CCommandHistory CommandHistory;

	public void HandleNavigation()
	{
		#region [ HHx2: Change difficulty ]

		if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HH) ||
		    CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HHO))
		{
			// [HH]x2 難易度変更
			CommandHistory.Add(EInstrumentPart.DRUMS, EPadFlag.HH);
			EPadFlag[] comChangeDifficulty = [EPadFlag.HH, EPadFlag.HH];
			if (CommandHistory.CheckCommand(comChangeDifficulty, EInstrumentPart.DRUMS))
			{
				CDTXMania.StageManager.stageSongSelectionNew.IncrementDifficultyLevel();
			}
		}

		#endregion

		#region [ Bx2 Guitar: Change difficulty ]

		if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.B)) // #24177 2011.1.17 yyagi || -> &&
		{
			// [B]x2 ギター難易度変更
			CommandHistory.Add(EInstrumentPart.GUITAR, EPadFlag.B);
			EPadFlag[] comChangeDifficultyG = [EPadFlag.B, EPadFlag.B];
			if (CommandHistory.CheckCommand(comChangeDifficultyG, EInstrumentPart.GUITAR))
			{
				CDTXMania.StageManager.stageSongSelectionNew.IncrementDifficultyLevel();
			}
		}

		#endregion

		#region [ Bx2 Bass: Change difficulty ]

		if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.B)) // #24177 2011.1.17 yyagi || -> &&
		{
			// [B]x2 ベース難易度変更
			CommandHistory.Add(EInstrumentPart.BASS, EPadFlag.B);
			EPadFlag[] comChangeDifficultyB = [EPadFlag.B, EPadFlag.B];
			if (CommandHistory.CheckCommand(comChangeDifficultyB, EInstrumentPart.BASS))
			{
				CDTXMania.StageManager.stageSongSelectionNew.IncrementDifficultyLevel();
			}
		}

		#endregion

		#region [ Yx2 Guitar: Switch between guitar and bass ]

		if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.Y))
		{
			// Pick, Y, Y, Pick で、ギターとベースを入れ替え
			CommandHistory.Add(EInstrumentPart.GUITAR, EPadFlag.Y);
			EPadFlag[] comSwapGtBs1 = [EPadFlag.Y, EPadFlag.Y];
			if (CommandHistory.CheckCommand(comSwapGtBs1, EInstrumentPart.GUITAR))
			{
				CDTXMania.Skin.soundChange.tPlay();
				// ギターとベースのキーを入れ替え
				//CDTXMania.ConfigIni.SwapGuitarBassKeyAssign();
				CDTXMania.ConfigIni.bIsSwappedGuitarBass = !CDTXMania.ConfigIni.bIsSwappedGuitarBass;
				//actSongList.tSwapClearLamps();
			}
		}

		#endregion

		#region [ Yx2 Bass: Switch between guitar and bass ]

		if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.Y))
		{
			// ベース[Pick]: コマンドとしてEnqueue
			CommandHistory.Add(EInstrumentPart.BASS, EPadFlag.Y);
			// Pick, Y, Y, Pick で、ギターとベースを入れ替え
			EPadFlag[] comSwapGtBs1 = [EPadFlag.Y, EPadFlag.Y];
			if (CommandHistory.CheckCommand(comSwapGtBs1, EInstrumentPart.BASS))
			{
				CDTXMania.Skin.soundChange.tPlay();
				// ギターとベースのキーを入れ替え
				//CDTXMania.ConfigIni.SwapGuitarBassKeyAssign();
				CDTXMania.ConfigIni.bIsSwappedGuitarBass = !CDTXMania.ConfigIni.bIsSwappedGuitarBass;
				//actSongList.tSwapClearLamps();
			}
		}

		#endregion
	}
}