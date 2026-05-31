using DTXMania.Core;

namespace DTXMania;

public interface IPerfFire
{
    public void Start(ELane lane, bool bFillIn, bool b大波, bool b細波, int _nJudgeLinePosY_delta_Drums = 0,
        bool bDisplay = true);

    public int OnUpdateAndDraw();
    int iPosY { get; set; }
}