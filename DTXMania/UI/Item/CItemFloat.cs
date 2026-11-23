using System.Globalization;

namespace DTXMania.UI.Item;

internal class CItemFloat : CItemBase
{
    public float fCurrentValue;

    private readonly float fMinimumValue;
    private readonly float fMaximumValue;
    private readonly float fStep;

    public CItemFloat(string strItemName, float fMinimumValue, float fMaximumValue, float fStep, float fCurrentValue,
        string strDescriptionJp, string strDescriptionEn)
    {
        tInitialize(strItemName, EPanelType.Normal, strDescriptionJp, strDescriptionEn);

        eType = EType.Float;

        this.fMinimumValue = fMinimumValue;
        this.fMaximumValue = fMaximumValue;
        this.fStep = fStep;
        this.fCurrentValue = fCurrentValue;
    }

    // CItemBase 実装

    public override void tMoveItemValueToNext()
    {
        fCurrentValue = Stepped(fCurrentValue + fStep);
    }

    public override void tMoveItemValueToPrevious()
    {
        fCurrentValue = Stepped(fCurrentValue - fStep);
    }

    private float Stepped(float value)
    {
        float onStep = MathF.Round(value / fStep) * fStep;
        return Math.Clamp(onStep, fMinimumValue, fMaximumValue);
    }

    public override object GetCurrentValue()
    {
        return fCurrentValue;
    }

    public override string GetStringValue()
    {
        return fCurrentValue.ToString("0.0##", CultureInfo.InvariantCulture);
    }
}
