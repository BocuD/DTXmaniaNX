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
	public CScore[] arScore = new CScore[ 5 ];
	public string[] arDifficultyLabel = new string[ 5 ];
	public bool bDTXFilesで始まるフォルダ名のBOXである;
	public bool bBoxDefで作成されたBOXである
	{
		get => !bDTXFilesで始まるフォルダ名のBOXである;
		set => bDTXFilesで始まるフォルダ名のBOXである = !value;
	}
	public Color col文字色 = Color.White;
	public List<CSongListNode> listランダム用ノードリスト;
	public List<CSongListNode> list子リスト;
	public STHitRanges stDrumHitRanges = new STHitRanges(nDefaultSizeMs: -1);
	public STHitRanges stDrumPedalHitRanges = new STHitRanges(nDefaultSizeMs: -1);
	public STHitRanges stGuitarHitRanges = new STHitRanges(nDefaultSizeMs: -1);
	public STHitRanges stBassHitRanges = new STHitRanges(nDefaultSizeMs: -1);
	public int nスコア数;
	public string pathSetDefの絶対パス = "";
	public CSongListNode r親ノード;
	public int SetDefのブロック番号;
	public Stack<int> stackRandomPerformanceNumber = new Stack<int>();
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
		CSongListNode newNode = new CSongListNode();
		newNode.eNodeType = eNodeType;
		newNode.nID = nID;
		newNode.arDifficultyLabel = arDifficultyLabel;
		newNode.arScore = arScore;
		newNode.bDTXFilesで始まるフォルダ名のBOXである = bDTXFilesで始まるフォルダ名のBOXである;
		newNode.bBoxDefで作成されたBOXである = bBoxDefで作成されたBOXである;
		newNode.col文字色 = col文字色;
		newNode.listランダム用ノードリスト = listランダム用ノードリスト;
		newNode.list子リスト = list子リスト;
		newNode.stDrumHitRanges = stDrumHitRanges;
		newNode.stDrumPedalHitRanges = stDrumPedalHitRanges;
		newNode.stGuitarHitRanges = stGuitarHitRanges;
		newNode.stBassHitRanges = stBassHitRanges;
		newNode.nスコア数 = nスコア数;
		newNode.pathSetDefの絶対パス = pathSetDefの絶対パス;
		newNode.r親ノード = r親ノード;
		newNode.SetDefのブロック番号 = SetDefのブロック番号;
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