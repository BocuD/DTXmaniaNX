using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;

namespace DTXMania;

/// <summary>
/// The three instrument status panes (drums / guitar / bass). The panes are part of the serializable
/// layout, so they are not built in the ctor — that would double them up when a skin's json is
/// deserialized on top of a fresh instance. The code default builds them via
/// <see cref="BuildDefaultPanes"/>; this panel just drives whichever panes the layout gave it.
/// </summary>
public class StatusPanel : UIGroup
{
	public StatusPanel() : base("StatusPanel")
	{
	}

	public void BuildDefaultPanes()
	{
		AddPane(EInstrumentPart.DRUMS, new Vector3(430, 720, 0));
		AddPane(EInstrumentPart.GUITAR, new Vector3(200, 720, 0));
		AddPane(EInstrumentPart.BASS, new Vector3(430, 720, 0));
	}

	private void AddPane(EInstrumentPart instrument, Vector3 position)
	{
		AddChild(new StatusPane
		{
			instrument = instrument,
			component = "Components/StatusPane.json",
			name = instrument.ToString(),
			position = position
		});
	}

	//each pane knows its own instrument, so a skin is free to rename or reorder them. Walked as a plain
	//loop rather than children.OfType<>() because Draw runs every frame and LINQ would allocate.
	public void SelectionChanged(SongNode? song, CChartData? chart)
	{
		foreach (UIDrawable child in children)
		{
			if (child is StatusPane pane)
			{
				pane.song = song;
			}
		}
	}

	public override void Draw(Matrix4x4 parentMatrix)
	{
		bool drumsMode = CDTXMania.ConfigIni.bDrumsEnabled;

		foreach (UIDrawable child in children)
		{
			if (child is StatusPane pane)
			{
				pane.isVisible = pane.instrument == EInstrumentPart.DRUMS ? drumsMode : !drumsMode;
			}
		}

		base.Draw(parentMatrix);
	}

}
