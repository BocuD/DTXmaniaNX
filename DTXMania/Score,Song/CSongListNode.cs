using System.Drawing;
using DTXMania.Core;

namespace DTXMania;

[Serializable]
internal class CSongListNode
{
	// プロパティ

	public ENodeType eNodeType = ENodeType.UNKNOWN;
	public enum ENodeType
	{
		SCORE,
		SCORE_MIDI,
		BOX,
		BACKBOX,
		RANDOM,
		UNKNOWN
	}
	public int nID { get; private set; }
	public CChartData[] arScore = new CChartData[ 5 ];
	public string[] arDifficultyLabel = new string[ 5 ];
	public bool bDTXFilesで始まるフォルダ名のBOXである;
	public bool bBoxDefで作成されたBOXである
	{
		get => !bDTXFilesで始まるフォルダ名のBOXである;
		set => bDTXFilesで始まるフォルダ名のBOXである = !value;
	}
	public Color col文字色 = Color.White;
	public List<CSongListNode> listランダム用ノードリスト;
	public List<CSongListNode> listChildNodes;
	public STHitRanges stDrumHitRanges = new(nDefaultSizeMs: -1);
	public STHitRanges stDrumPedalHitRanges = new(nDefaultSizeMs: -1);
	public STHitRanges stGuitarHitRanges = new(nDefaultSizeMs: -1);
	public STHitRanges stBassHitRanges = new(nDefaultSizeMs: -1);
	public int nChartCount;
	public string pathSetDefPath = "";
	public CSongListNode parentNode;
	public int SetDefBlockNumber;
	public Stack<int> stackRandomPerformanceNumber = new();
	public string strGenre = "";
	public string strTitle = "";
	public string strBreadcrumbs = "";		// #27060 2011.2.27 yyagi; MUSIC BOXのパンくずリスト (曲リスト構造内の絶対位置捕捉のために使う)
	public string strSkinPath = "";			// #28195 2012.5.4 yyagi; box.defでのスキン切り替え対応
	public string strVersion = "";
		
	// コンストラクタ

	public CSongListNode()
	{
		nID = id++;
	}

	//
	public CSongListNode ShallowCopyOfSelf()
	{
		CSongListNode newNode = new();
		newNode.eNodeType = eNodeType;
		newNode.nID = nID;
		newNode.arDifficultyLabel = arDifficultyLabel;
		newNode.arScore = arScore;
		newNode.bDTXFilesで始まるフォルダ名のBOXである = bDTXFilesで始まるフォルダ名のBOXである;
		newNode.bBoxDefで作成されたBOXである = bBoxDefで作成されたBOXである;
		newNode.col文字色 = col文字色;
		newNode.listランダム用ノードリスト = listランダム用ノードリスト;
		newNode.listChildNodes = listChildNodes;
		newNode.stDrumHitRanges = stDrumHitRanges;
		newNode.stDrumPedalHitRanges = stDrumPedalHitRanges;
		newNode.stGuitarHitRanges = stGuitarHitRanges;
		newNode.stBassHitRanges = stBassHitRanges;
		newNode.nChartCount = nChartCount;
		newNode.pathSetDefPath = pathSetDefPath;
		newNode.parentNode = parentNode;
		newNode.SetDefBlockNumber = SetDefBlockNumber;
		newNode.stackRandomPerformanceNumber = stackRandomPerformanceNumber;
		newNode.strGenre = strGenre;
		newNode.strTitle = strTitle;
		newNode.strVersion = strVersion;
		newNode.strBreadcrumbs = strBreadcrumbs;
		newNode.strSkinPath = strSkinPath;

		return newNode;
	}

	// Other

	#region [ private ]
	//-----------------
	private static int id;
	//-----------------
	#endregion
}