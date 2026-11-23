using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using FDK;

namespace DTXMania;

internal partial class CStagePerfGuitarScreen
{
	protected override void UpdateChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip,
		EInstrumentPart inst)
	{
		int instIndex = (int)inst;

		GuitarPerformance guitar = GuitarFor(inst)!;
		guitar.UpdateNote(pChip);
		ApplyGuitarEvents(guitar);

		if (CDTXMania.ConfigIni.bGraph有効[instIndex] && guitar.Data.HasTargetGhost)
		{
			actGraph.dbGraphValue_Goal = guitar.Data.CalculateTargetGhostGoal();
		}
	}
}