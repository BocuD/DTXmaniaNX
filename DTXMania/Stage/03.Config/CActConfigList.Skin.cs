using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania;

internal partial class CActConfigList
{
    private CItemList iSystemSkinSubfolder;
        
    private BaseTexture? txSkinSample;				// #28195 2012.5.2 yyagi
    
    private void tGenerateSkinSample()
    {
        nSkinIndex = iSystemSkinSubfolder.nCurrentlySelectedIndex;
        if (nSkinSampleIndex != nSkinIndex)
        {
            string path = skinSubFolders[nSkinIndex];
            path = Path.Combine(path, @"Graphics\2_background.jpg");
            
            //todo: Change resolution to proper size (will we even keep this system in place??? lol)
            //254 * 143
            txSkinSample = BaseTexture.LoadFromPath(path);
            nSkinSampleIndex = nSkinIndex;
        }
    }
    
    private string[] skinSubFolders;			//
    private string[] skinNames;					//
    private string skinSubFolder_org;			//
    private int nSkinSampleIndex;				//
    private int nSkinIndex;						//
        
    private void ScanSkinFolders()
    {
        int ns = (CDTXMania.Skin.strSystemSkinSubfolders == null) ? 0 : CDTXMania.Skin.strSystemSkinSubfolders.Length;
        int nb = (CDTXMania.Skin.strBoxDefSkinSubfolders == null) ? 0 : CDTXMania.Skin.strBoxDefSkinSubfolders.Length;
            
        skinSubFolders = new string[ns + nb];
        for (int i = 0; i < ns; i++)
        {
            skinSubFolders[i] = CDTXMania.Skin.strSystemSkinSubfolders[i];
        }
        for (int i = 0; i < nb; i++)
        {
            skinSubFolders[ns + i] = CDTXMania.Skin.strBoxDefSkinSubfolders[i];
        }
        skinSubFolder_org = CDTXMania.Skin.GetCurrentSkinSubfolderFullName(true);
        Array.Sort(skinSubFolders);
        skinNames = CSkin.GetSkinName(skinSubFolders);
        nSkinIndex = Array.BinarySearch(skinSubFolders, skinSubFolder_org);
        if (nSkinIndex < 0)	// 念のため
        {
            nSkinIndex = 0;
        }
        nSkinSampleIndex = -1;
    }
    
    #region [ NEW SKIN ]
    private CItemList iNewSkinSelector;
    private string[] newSkinNames;
    private int nNewSkinIndex;

    private void ScanNewSkinData()
    {
        CDTXMania.SkinManager.ScanSkinDirectory();
        newSkinNames = CDTXMania.SkinManager.skins.Select(x => x.name).Prepend("None").ToArray();
        
        //find current skin index
        for (int i = 0; i < CDTXMania.SkinManager.skins.Count; i++)
        {
            var skin = CDTXMania.SkinManager.skins[i];
            if (skin.name == CDTXMania.SkinManager.currentSkin?.name)
            {
                nNewSkinIndex = i + 1; //account for none
                break;
            }
        }
    }
    
    private void ApplySkinChanges()
    {
        //Apply skin changes
        if (iNewSkinSelector.nCurrentlySelectedIndex != 0) //0 is none
        {
            CDTXMania.SkinManager.ChangeSkin(CDTXMania.SkinManager.skins[iNewSkinSelector.nCurrentlySelectedIndex - 1]); //account for none
        }
        else
        {
            CDTXMania.SkinManager.ChangeSkin(null);
        }
    }
    #endregion
    
    private void tSetupItemList_Skin()
    {
        listItems.Clear();

        ScanSkinFolders();
        ScanNewSkinData();
        
        iSystemSkinSubfolder = new CItemList("Skin (Legacy)", CItemBase.EPanelType.Normal, nSkinIndex,
            "スキン切替：スキンを切り替えます。\n" +
            "\n",
            "Choose skin",
            skinNames);
        iSystemSkinSubfolder.BindConfig(() =>
            {
                //Handle updating of CDTXMania.ConfigIni.strSystemSkinSubfolderFullName back to UI value
                int nSkinIndex = -1;
                for (int i = 0; i < skinSubFolders.Length; i++)
                {
                    if (skinSubFolders[i] == CDTXMania.ConfigIni.strSystemSkinSubfolderFullName) {
                        nSkinIndex = i;
                        break;
                    }
                }
                
                if (nSkinIndex != -1) {

                    iSystemSkinSubfolder.nCurrentlySelectedIndex = nSkinIndex;
                    this.nSkinIndex = nSkinIndex;
                    CDTXMania.Skin.SetCurrentSkinSubfolderFullName(CDTXMania.ConfigIni.strSystemSkinSubfolderFullName, true);
                }
            },
            () => { });
        iSystemSkinSubfolder.action = tGenerateSkinSample;
        listItems.Add(iSystemSkinSubfolder);
        
        iNewSkinSelector = new CItemList("Skin (New)", CItemBase.EPanelType.Normal, nNewSkinIndex,
            "スキン切替：スキンを切り替えます。\n" +
            "\n",
            "Choose skin",
            newSkinNames);
        listItems.Add(iNewSkinSelector);

        CItemToggle iSystemUseBoxDefSkin = new("Skin (Box)", CDTXMania.ConfigIni.bUseBoxDefSkin,
            "Music boxスキンの利用：\n" +
            "特別なスキンが設定されたMusic box\n" +
            "に出入りしたときに、自動でスキンを\n" +
            "切り替えるかどうかを設定します。\n",
            "Box skin:\n" +
            "Automatically change skin as per box.def file.");
        iSystemUseBoxDefSkin.BindConfig(
            () => iSystemUseBoxDefSkin.bON = CDTXMania.ConfigIni.bUseBoxDefSkin,
            () => CDTXMania.ConfigIni.bUseBoxDefSkin = iSystemUseBoxDefSkin.bON);
        iSystemUseBoxDefSkin.action = () => CSkin.bUseBoxDefSkin = iSystemUseBoxDefSkin.bON;
        listItems.Add(iSystemUseBoxDefSkin);
       
        tAddReturnToMenuItem(tSetupItemList_System);

        tGenerateSkinSample();

        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemSkin;
    }
}