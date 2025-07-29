using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal partial class CActConfigList
{
    /*
     *  nVelocityMin.LC = 0;
		nVelocityMin.HH = 20;
		nVelocityMin.SD = 0;
		nVelocityMin.BD = 0;
		nVelocityMin.HT = 0;
		nVelocityMin.LT = 0;
		nVelocityMin.FT = 0;
		nVelocityMin.CY = 0;
		nVelocityMin.RD = 0;
		nVelocityMin.LP = 0;
		nVelocityMin.LBD = 0;
     */
    public void tSetupItemList_DrumsVelocity()
    {
        listItems.Clear();

        CItemBase iVelocityDrumsReturnToMenu = new("<< ReturnTo Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_Drums
        };
        listItems.Add(iVelocityDrumsReturnToMenu);

        AddDrumVelocityItem("Left cymbal", 
			() => CDTXMania.ConfigIni.nVelocityMin.LC, 
			value => CDTXMania.ConfigIni.nVelocityMin.LC = value);
        AddDrumVelocityItem("Hi-hat", 
	        () => CDTXMania.ConfigIni.nVelocityMin.HH, 
	        value => CDTXMania.ConfigIni.nVelocityMin.HH = value);
        AddDrumVelocityItem("Snare drum", 
	        () => CDTXMania.ConfigIni.nVelocityMin.SD, 
	        value => CDTXMania.ConfigIni.nVelocityMin.SD = value);
        AddDrumVelocityItem("Bass drum", 
	        () => CDTXMania.ConfigIni.nVelocityMin.BD, 
	        value => CDTXMania.ConfigIni.nVelocityMin.BD = value);
        AddDrumVelocityItem("High tom", 
	        () => CDTXMania.ConfigIni.nVelocityMin.HT, 
	        value => CDTXMania.ConfigIni.nVelocityMin.HT = value);
        AddDrumVelocityItem("Low tom", 
	        () => CDTXMania.ConfigIni.nVelocityMin.LT, 
	        value => CDTXMania.ConfigIni.nVelocityMin.LT = value);
        AddDrumVelocityItem("Floor tom", 
	        () => CDTXMania.ConfigIni.nVelocityMin.FT, 
	        value => CDTXMania.ConfigIni.nVelocityMin.FT = value);
        AddDrumVelocityItem("Cymbal", 
	        () => CDTXMania.ConfigIni.nVelocityMin.CY, 
	        value => CDTXMania.ConfigIni.nVelocityMin.CY = value);
        AddDrumVelocityItem("Ride cymbal", 
	        () => CDTXMania.ConfigIni.nVelocityMin.RD, 
	        value => CDTXMania.ConfigIni.nVelocityMin.RD = value);
        AddDrumVelocityItem("Left pedal", 
	        () => CDTXMania.ConfigIni.nVelocityMin.LP, 
	        value => CDTXMania.ConfigIni.nVelocityMin.LP = value);
        AddDrumVelocityItem("Left bass drum", 
	        () => CDTXMania.ConfigIni.nVelocityMin.LBD, 
	        value => CDTXMania.ConfigIni.nVelocityMin.LBD = value);

        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.VelocityDrums;
    }
    
    private void AddDrumVelocityItem(string name, Func<int> get, Action<int> set)
    {
	    CItemInteger iVelocityAdjust = new($"{name}", 0, 100, get(),
		    $"{name} ドラムの最小速度を調整します。\n" +
		    $"0から100の範囲で選択できます。",
		    $"Adjust the minimum hit velocity threshold of the {name}.\n" +
		    $"A range from 0 to 100 can be selected.");
	    iVelocityAdjust.BindConfig(
		    () => iVelocityAdjust.nCurrentValue = get(),
		    () => set(iVelocityAdjust.nCurrentValue));
	    listItems.Add(iVelocityAdjust);
    }
}