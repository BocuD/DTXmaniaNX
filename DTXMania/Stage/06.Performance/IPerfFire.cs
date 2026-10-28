using DTXMania.Core;

namespace DTXMania;

public interface IPerfFire
{
    void Start(ELane lane, bool fillIn, bool bigWaves, bool smallWaves, int judgeLineOffset = 0, bool display = true);

    int iPosY { get; set; }
}
