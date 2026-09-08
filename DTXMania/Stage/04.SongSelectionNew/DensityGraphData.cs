using DTXMania.Core;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

public sealed class DensityGraphData(DensityGraph owner)
{
    [DataField] public bool IsDrums => owner.instrument == EInstrumentPart.DRUMS;
    [DataField] public bool IsGuitarBass => !IsDrums;

    [DataField] public string NoteCount => owner.noteCount > 0 ? owner.noteCount.ToString() : string.Empty;

    [DataField] public double NoteCountX => IsDrums ? 150.0 : 102.0;
}
