using DTXMania.Core;

namespace DTXMania;

public interface IPerfFire
{
    void Start(ELane lane, bool fillIn, bool fillInEnd);

    int iPosY { get; set; }
}
