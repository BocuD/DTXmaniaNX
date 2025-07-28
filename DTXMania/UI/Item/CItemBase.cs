using DTXMania.Core;

namespace DTXMania.UI.Item;

/// <summary>
/// すべてのアイテムの基本クラス。
/// Base class for all items.
/// </summary>
internal class CItemBase
{
	// プロパティ

	public EPanelType ePanelType;
	public enum EPanelType
	{
		Normal,
		Folder,
		Other
	}

	public EType eType;
	public enum EType
	{
		基本形,
		ONorOFFToggle,
		ONorOFForUndefined3State,
		Integer,
		List,
		切替リスト
	}

	public string strItemName;
	public string strDescription;


	// コンストラクタ

	public CItemBase()
	{
		strItemName = "";
		strDescription = "";
	}
		
	public CItemBase(string strItemName, string strDescriptionJp, string strDescriptionEn)
		: this() {
		tInitialize(strItemName, EPanelType.Normal, strDescriptionJp, strDescriptionEn);
	}

	public CItemBase(string strItemName, EPanelType ePanelType, string strDescriptionJp, string strDescriptionEn)
		: this() {
		tInitialize(strItemName, ePanelType, strDescriptionJp, strDescriptionEn);
	}
		
	// メソッド；子クラスで実装する
		
	//This will allow simplifying the code inside CActConfigList.cs
	public Action action;

	public void RunAction()
	{
		tEnterPressed();

		action?.Invoke();
	}

	//existing method which gets inherited by CItemInteger, CItemList, etc
	protected virtual void tEnterPressed()
	{
	}
	public virtual void tMoveItemValueToNext()
	{
	}
	public virtual void tMoveItemValueToPrevious()
	{
	}

	public void tInitialize(string strItemName, EPanelType ePanelType, string strDescriptionJp, string strDescriptionEn) {
		this.strItemName = strItemName;
		this.ePanelType = ePanelType;
		strDescription = CDTXMania.isJapanese ? strDescriptionJp : strDescriptionEn;
	}
	public virtual object GetCurrentValue()
	{
		return null;
	}
	public virtual int GetIndex()
	{
		return 0;
	}
	public virtual void SetIndex( int index )
	{
	}

	private Action _readFromConfig;
	public virtual void ReadFromConfig()
	{
		_readFromConfig?.Invoke();
	}

	private Action _writeToConfig;
	public void WriteToConfig()
	{
		_writeToConfig?.Invoke();
	}
		
	public void BindConfig(Action readFromConfig, Action writeToConfig)
	{
		_readFromConfig = readFromConfig;
		_writeToConfig = writeToConfig;
	}

	public virtual string GetStringValue()
	{
		return string.Empty;
	}
}