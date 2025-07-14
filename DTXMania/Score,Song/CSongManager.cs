using System.Text;
using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;

namespace DTXMania;

[Serializable]
internal class CSongManager
{
	// Properties

	public int nNbScoresFromSongsDB
	{
		get; 
		set; 
	}
	public int nNbScoresForSongsDB
	{
		get;
		set;
	}
	public int nNbScoresFromScoreCache 
	{
		get;
		set; 
	}
	public int nNbScoresFromFile
	{
		get;
		set;
	}
	public int nNbScoresFound 
	{ 
		get;
		set;
	}
	public int nNbSongNodesFound
	{
		get; 
		set;
	}
	[NonSerialized]
	public List<CScore> listSongsDB;					// songs.dbから構築されるlist
	public List<CSongListNode> listSongRoot;            // 起動時にフォルダ検索して構築されるlist
	public List<CSongListNode> listSongBeforeSearch = null;    // The SongListNode before a search is performed
	public bool bIsSuspending							// 外部スレッドから、内部スレッドのsuspendを指示する時にtrueにする
	{													// 再開時は、これをfalseにしてから、次のautoReset.Set()を実行する
		get;
		set;
	}
	public bool bIsSlowdown								// #PREMOVIE再生時に曲検索を遅くする
	{
		get;
		set;
	}
	[NonSerialized]
	private AutoResetEvent autoReset;
	public AutoResetEvent AutoReset
	{
		get => autoReset;
		private set => autoReset = value;
	}

	private int searchCount;							// #PREMOVIE中は検索n回実行したら少しスリープする

	// コンストラクタ

	public CSongManager()
	{
		listSongsDB = new List<CScore>();
		listSongRoot = new List<CSongListNode>();
		nNbSongNodesFound = 0;
		nNbScoresFound = 0;
		bIsSuspending = false;						// #27060
		autoReset = new AutoResetEvent( true );	// #27060
		searchCount = 0;
	}


	// Methods

	#region [ Read SongsDB(songs.db) ]
	//-----------------
	public void tReadSongsDB( string SongsDBFilename )
	{
		nNbScoresFromSongsDB = 0;
		if( File.Exists( SongsDBFilename ) )
		{
			BinaryReader br = null;
			try
			{
				br = new BinaryReader( File.OpenRead( SongsDBFilename ) );
				if ( !br.ReadString().Equals( SONGSDB_VERSION ) )
				{
					throw new InvalidDataException( "ヘッダが異なります。" );
				}
				listSongsDB = [];

				while( true )
				{
					try
					{
						CScore item = tReadOneScoreFromSongsDB( br );
						listSongsDB.Add( item );
						nNbScoresFromSongsDB++;
					}
					catch( EndOfStreamException )
					{
						break;
					}
				}
			}
			finally
			{
				if( br != null )
					br.Close();
			}
		}
	}
	//-----------------
	#endregion

	#region [ Search songs and create a list ]
	//-----------------
	public void tSearchSongsAndCreateList( string strBaseFolder )
	{
		tSearchSongsAndCreateList( strBaseFolder, listSongRoot, null );
	}

	private void tSearchSongsAndCreateList(string strBaseFolder, List<CSongListNode> listNodeList,
		CSongListNode nodeParent)
	{
		if (!strBaseFolder.EndsWith(@"\"))
			strBaseFolder += @"\";

		DirectoryInfo info = new(strBaseFolder);

		if (CDTXMania.ConfigIni.bLogSongSearch)
			Trace.TraceInformation("基点フォルダ: " + strBaseFolder);

		#region [ a.フォルダ内に set.def が存在する場合 → set.def からノード作成]

		//If set.def exists in the folder, create nodes from set.def
		//-----------------------------
		string path = strBaseFolder + "set.def";
		if (File.Exists(path))
		{
			ParseSetDef(strBaseFolder, listNodeList, nodeParent, path);
		}
		//-----------------------------

		#endregion

		#region [ b.フォルダ内に set.def が存在しない場合 → 個別ファイルからノード作成 ]

		// If set.def does not exist in the folder, create nodes from individual files
		//-----------------------------
		else
		{
			//loop over all files, try to load them as charts
			foreach (FileInfo fileinfo in info.GetFiles())
			{
				SlowOrSuspendSearchTask(); // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす
				string strExt = fileinfo.Extension.ToLower();
				if (strExt.Equals(".dtx") || strExt.Equals(".gda") || strExt.Equals(".g2d") || strExt.Equals(".bms") ||
				    strExt.Equals(".bme"))
				{
					AddSongChart(strBaseFolder, listNodeList, nodeParent, fileinfo);
				}
				else if (strExt.Equals(".mid") || strExt.Equals(".smf"))
				{
					// DoNothing
					//????
				}
			}
		}

		//-----------------------------

		#endregion

		//scan subdirectories
		foreach (DirectoryInfo infoDir in info.GetDirectories())
		{
			SlowOrSuspendSearchTask(); // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

			#region [ a. "dtxfiles." で始まるフォルダの場合 ]

			//-----------------------------
			if (infoDir.Name.ToLower().StartsWith("dtxfiles."))
			{
				CSongListNode cSongListNode = new()
				{
					eNodeType = CSongListNode.ENodeType.BOX,
					bDTXFilesで始まるフォルダ名のBOXである = true,
					strTitle = infoDir.Name.Substring(9),
					nスコア数 = 1,
					parentNode = nodeParent
				};

				// 一旦、上位BOXのスキン情報をコピー (後でbox.defの記載にて上書きされる場合がある)
				cSongListNode.strSkinPath =
					cSongListNode.parentNode == null ? "" : cSongListNode.parentNode.strSkinPath;

				cSongListNode.strBreadcrumbs = cSongListNode.parentNode == null
					? cSongListNode.strTitle
					: cSongListNode.parentNode.strBreadcrumbs + " > " + cSongListNode.strTitle;


				cSongListNode.listChildNodes = [];
				cSongListNode.arScore[0] = new CScore();
				cSongListNode.arScore[0].FileInformation.AbsoluteFolderPath = infoDir.FullName + @"\";
				cSongListNode.arScore[0].SongInformation.Title = cSongListNode.strTitle;
				cSongListNode.arScore[0].SongInformation.Comment = CDTXMania.isJapanese ? "BOX に移動します。" : "Enter into the BOX.";
				
				listNodeList.Add(cSongListNode);
				if (File.Exists(infoDir.FullName + @"\box.def"))
				{
					CBoxDef boxdef = new(infoDir.FullName + @"\box.def");
					if (boxdef.Title is { Length: > 0 })
					{
						cSongListNode.strTitle = boxdef.Title;
					}

					if (boxdef.Genre is { Length: > 0 })
					{
						cSongListNode.strGenre = boxdef.Genre;
					}

					if (boxdef.Color != Color.White)
					{
						cSongListNode.col文字色 = boxdef.Color;
					}

					if (boxdef.Artist is { Length: > 0 })
					{
						cSongListNode.arScore[0].SongInformation.ArtistName = boxdef.Artist;
					}

					if (boxdef.Comment is { Length: > 0 })
					{
						cSongListNode.arScore[0].SongInformation.Comment = boxdef.Comment;
					}

					if (boxdef.Preimage is { Length: > 0 })
					{
						cSongListNode.arScore[0].SongInformation.Preimage = boxdef.Preimage;
					}

					if (boxdef.Premovie is { Length: > 0 })
					{
						cSongListNode.arScore[0].SongInformation.Premovie = boxdef.Premovie;
					}

					if (boxdef.Presound is { Length: > 0 })
					{
						cSongListNode.arScore[0].SongInformation.Presound = boxdef.Presound;
					}

					if (boxdef.SkinPath != null)
					{
						if (boxdef.SkinPath == "")
						{
							// box.defにスキン情報が記載されていないなら、上位BOXのスキン情報をコピー
							cSongListNode.strSkinPath = cSongListNode.parentNode == null
								? ""
								: cSongListNode.parentNode.strSkinPath;
						}
						else
						{
							// box.defに記載されているスキン情報をコピー。末尾に必ず\をつけておくこと。
							string s = Path.Combine(infoDir.FullName, boxdef.SkinPath);
							if (s[s.Length - 1] != Path.DirectorySeparatorChar) // フォルダ名末尾に\を必ずつけて、CSkin側と表記を統一する
							{
								s += Path.DirectorySeparatorChar;
							}

							if (CDTXMania.Skin.bIsValid(s))
							{
								cSongListNode.strSkinPath = s;
							}
							else
							{
								cSongListNode.strSkinPath = cSongListNode.parentNode == null
									? ""
									: cSongListNode.parentNode.strSkinPath;
							}
						}
					}

					// copy hit ranges from the box.def
					// these can always be copied regardless of being set,
					// as song list nodes and boxdefs use the same method to indicate an unset range
					cSongListNode.stDrumHitRanges = boxdef.stDrumHitRanges;
					cSongListNode.stDrumPedalHitRanges = boxdef.stDrumPedalHitRanges;
					cSongListNode.stGuitarHitRanges = boxdef.stGuitarHitRanges;
					cSongListNode.stBassHitRanges = boxdef.stBassHitRanges;
				}

				if (CDTXMania.ConfigIni.bLogSongSearch)
				{
					Trace.Indent();
					try
					{
						StringBuilder sb = new(0x100);
						sb.Append($"nID#{cSongListNode.nID:D3}");
						if (cSongListNode.parentNode != null)
						{
							sb.Append($"(in#{cSongListNode.parentNode.nID:D3}):");
						}
						else
						{
							sb.Append("(onRoot):");
						}

						sb.Append(" BOX, Title=" + cSongListNode.strTitle);
						sb.Append(", Folder=" + cSongListNode.arScore[0].FileInformation.AbsoluteFolderPath);
						sb.Append(", Comment=" + cSongListNode.arScore[0].SongInformation.Comment);
						sb.Append(", SkinPath=" + cSongListNode.strSkinPath);
						Trace.TraceInformation(sb.ToString());
					}
					finally
					{
						Trace.Unindent();
					}
				}

				tSearchSongsAndCreateList(infoDir.FullName + @"\", cSongListNode.listChildNodes, cSongListNode);
			}
			//-----------------------------

			#endregion

			#region [ b.box.def を含むフォルダの場合  ]

			//if the folder contains a box.def file, handle it differently
			else if (File.Exists(infoDir.FullName + @"\box.def"))
			{
				LoadBoxDef(listNodeList, nodeParent, infoDir);
			}
			//-----------------------------

			#endregion

			#region [ c.通常フォルダの場合 ]

			//-----------------------------
			else
			{
				tSearchSongsAndCreateList(infoDir.FullName + @"\", listNodeList, nodeParent);
			}

			//-----------------------------

			#endregion
		}
	}

	private void LoadBoxDef(List<CSongListNode> listNodeList, CSongListNode nodeParent, DirectoryInfo infoDir)
	{
		CBoxDef boxdef = new(infoDir.FullName + @"\box.def");
		CSongListNode cSongListNode = new()
		{
			eNodeType = CSongListNode.ENodeType.BOX,
			bDTXFilesで始まるフォルダ名のBOXである = false,
			strTitle = boxdef.Title,
			strGenre = boxdef.Genre,
			col文字色 = boxdef.Color,
			nスコア数 = 1,
			arScore =
			{
				[0] = new CScore()
			}
		};
		cSongListNode.arScore[0].FileInformation.AbsoluteFolderPath = infoDir.FullName + @"\";
		cSongListNode.arScore[0].SongInformation.Title = boxdef.Title;
		cSongListNode.arScore[0].SongInformation.Genre = boxdef.Genre;
		cSongListNode.arScore[0].SongInformation.ArtistName = boxdef.Artist;
		cSongListNode.arScore[0].SongInformation.Comment = boxdef.Comment;
		cSongListNode.arScore[0].SongInformation.Preimage = boxdef.Preimage;
		cSongListNode.arScore[0].SongInformation.Premovie = boxdef.Premovie;
		cSongListNode.arScore[0].SongInformation.Presound = boxdef.Presound;
		cSongListNode.parentNode = nodeParent;

		if (boxdef.SkinPath == "")
		{
			// box.defにスキン情報が記載されていないなら、上位BOXのスキン情報をコピー
			cSongListNode.strSkinPath =
				cSongListNode.parentNode == null ? "" : cSongListNode.parentNode.strSkinPath;
		}
		else
		{
			// box.defに記載されているスキン情報をコピー。末尾に必ず\をつけておくこと。
			string s = Path.Combine(infoDir.FullName, boxdef.SkinPath);
			if (s[s.Length - 1] != Path.DirectorySeparatorChar) // フォルダ名末尾に\を必ずつけて、CSkin側と表記を統一する
			{
				s += Path.DirectorySeparatorChar;
			}

			if (CDTXMania.Skin.bIsValid(s))
			{
				cSongListNode.strSkinPath = s;
			}
			else
			{
				cSongListNode.strSkinPath =
					cSongListNode.parentNode == null ? "" : cSongListNode.parentNode.strSkinPath;
			}
		}

		cSongListNode.strBreadcrumbs = cSongListNode.parentNode == null
			? cSongListNode.strTitle
			: cSongListNode.parentNode.strBreadcrumbs + " > " + cSongListNode.strTitle;


		cSongListNode.listChildNodes = [];
		cSongListNode.stDrumHitRanges = boxdef.stDrumHitRanges;
		cSongListNode.stDrumPedalHitRanges = boxdef.stDrumPedalHitRanges;
		cSongListNode.stGuitarHitRanges = boxdef.stGuitarHitRanges;
		cSongListNode.stBassHitRanges = boxdef.stBassHitRanges;
		listNodeList.Add(cSongListNode);

		if (CDTXMania.ConfigIni.bLogSongSearch)
		{
			Trace.TraceInformation("box.def検出 : {0}", infoDir.FullName + @"\box.def");
			Trace.Indent();
			try
			{
				StringBuilder sb = new(0x400);
				sb.Append($"nID#{cSongListNode.nID:D3}");
				if (cSongListNode.parentNode != null)
				{
					sb.Append($"(in#{cSongListNode.parentNode.nID:D3}):");
				}
				else
				{
					sb.Append("(onRoot):");
				}

				sb.Append("BOX, Title=" + cSongListNode.strTitle);
				if (cSongListNode.strGenre is { Length: > 0 })
				{
					sb.Append(", Genre=" + cSongListNode.strGenre);
				}

				if (cSongListNode.arScore[0].SongInformation.ArtistName is { Length: > 0 })
				{
					sb.Append(", Artist=" + cSongListNode.arScore[0].SongInformation.ArtistName);
				}

				if (cSongListNode.arScore[0].SongInformation.Comment is { Length: > 0 })
				{
					sb.Append(", Comment=" + cSongListNode.arScore[0].SongInformation.Comment);
				}

				if (cSongListNode.arScore[0].SongInformation.Preimage is { Length: > 0 })
				{
					sb.Append(", Preimage=" + cSongListNode.arScore[0].SongInformation.Preimage);
				}

				if (cSongListNode.arScore[0].SongInformation.Premovie is { Length: > 0 })
				{
					sb.Append(", Premovie=" + cSongListNode.arScore[0].SongInformation.Premovie);
				}

				if (cSongListNode.arScore[0].SongInformation.Presound is { Length: > 0 })
				{
					sb.Append(", Presound=" + cSongListNode.arScore[0].SongInformation.Presound);
				}

				if (cSongListNode.col文字色 != ColorTranslator.FromHtml("White"))
				{
					sb.Append(", FontColor=" + cSongListNode.col文字色);
				}

				// hit ranges
				tTryAppendHitRanges(cSongListNode.stDrumHitRanges, @"Drum", sb);
				tTryAppendHitRanges(cSongListNode.stDrumPedalHitRanges, @"DrumPedal", sb);
				tTryAppendHitRanges(cSongListNode.stGuitarHitRanges, @"Guitar", sb);
				tTryAppendHitRanges(cSongListNode.stBassHitRanges, @"Bass", sb);

				if (cSongListNode.strSkinPath is { Length: > 0 })
				{
					sb.Append(", SkinPath=" + cSongListNode.strSkinPath);
				}

				Trace.TraceInformation(sb.ToString());
			}
			finally
			{
				Trace.Unindent();
			}
		}

		tSearchSongsAndCreateList(infoDir.FullName + @"\", cSongListNode.listChildNodes, cSongListNode);
	}

	private void AddSongChart(string strBaseFolder, List<CSongListNode> listNodeList, CSongListNode nodeParent, FileInfo fileinfo)
	{
		CSongListNode cSongListNode = new()
		{
			eNodeType = CSongListNode.ENodeType.SCORE,
			nスコア数 = 1,
			parentNode = nodeParent
		};

		cSongListNode.strBreadcrumbs = ( cSongListNode.parentNode == null ) ?
			strBaseFolder + fileinfo.Name : cSongListNode.parentNode.strBreadcrumbs + " > " + strBaseFolder + fileinfo.Name;

		cSongListNode.arScore[ 0 ] = new CScore();
		cSongListNode.arScore[ 0 ].FileInformation.AbsoluteFilePath = strBaseFolder + fileinfo.Name;
		cSongListNode.arScore[ 0 ].FileInformation.AbsoluteFolderPath = strBaseFolder;
		cSongListNode.arScore[ 0 ].FileInformation.FileSize = fileinfo.Length;
		cSongListNode.arScore[ 0 ].FileInformation.LastModified = fileinfo.LastWriteTime;
		string strFileNameScoreIni = cSongListNode.arScore[ 0 ].FileInformation.AbsoluteFilePath + ".score.ini";
		if( File.Exists( strFileNameScoreIni ) )
		{
			FileInfo infoScoreIni = new( strFileNameScoreIni );
			cSongListNode.arScore[ 0 ].ScoreIniInformation.FileSize = infoScoreIni.Length;
			cSongListNode.arScore[ 0 ].ScoreIniInformation.LastModified = infoScoreIni.LastWriteTime;
		}
		nNbScoresFound++;
		listNodeList.Add( cSongListNode );
		nNbSongNodesFound++;
		
		if( CDTXMania.ConfigIni.bLogSongSearch )
		{
			Trace.Indent();
			try
			{
				StringBuilder sb = new( 0x100 );
				sb.Append($"nID#{cSongListNode.nID:D3}");
				if( cSongListNode.parentNode != null )
				{
					sb.Append($"(in#{cSongListNode.parentNode.nID:D3}):");
				}
				else
				{
					sb.Append( "(onRoot):" );
				}
				sb.Append( " SONG, File=" + cSongListNode.arScore[ 0 ].FileInformation.AbsoluteFilePath );
				sb.Append( ", Size=" + cSongListNode.arScore[ 0 ].FileInformation.FileSize );
				sb.Append( ", LastUpdate=" + cSongListNode.arScore[ 0 ].FileInformation.LastModified );
				Trace.TraceInformation( sb.ToString() );
			}
			finally
			{
				Trace.Unindent();
			}
		}
	}

	private void ParseSetDef(string strBaseFolder, List<CSongListNode> listNodeList, CSongListNode nodeParent,
		string path)
	{
		CSetDef def = new(path);

		if (CDTXMania.ConfigIni.bLogSongSearch)
		{
			Trace.TraceInformation("set.def検出 : {0}", path);
			Trace.Indent();
		}

		try
		{
			SlowOrSuspendSearchTask(); // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす
			for (int i = 0; i < def.blocks.Count; i++)
			{
				CSetDef.CBlock block = def.blocks[i];
				CSongListNode item = new()
				{
					eNodeType = CSongListNode.ENodeType.SCORE,
					strTitle = block.Title,
					strGenre = block.Genre,
					nスコア数 = 0,
					col文字色 = block.FontColor,
					SetDefのブロック番号 = i,
					pathSetDefの絶対パス = path,
					parentNode = nodeParent
				};

				item.strBreadcrumbs = (item.parentNode == null) ? path + i : item.parentNode.strBreadcrumbs + " > " + path + i;

				for (int j = 0; j < 5; j++)
				{
					if (!string.IsNullOrEmpty(block.File[j]))
					{
						string str2 = strBaseFolder + block.File[j];
						if (File.Exists(str2))
						{
							item.arDifficultyLabel[j] = block.Label[j];
							item.arScore[j] = new CScore();
							item.arScore[j].FileInformation.AbsoluteFilePath = str2;
							item.arScore[j].FileInformation.AbsoluteFolderPath =
								Path.GetFullPath(Path.GetDirectoryName(str2)) + @"\";
							FileInfo info2 = new(str2);
							item.arScore[j].FileInformation.FileSize = info2.Length;
							item.arScore[j].FileInformation.LastModified = info2.LastWriteTime;
							string str3 = str2 + ".score.ini";
							if (File.Exists(str3))
							{
								FileInfo info3 = new(str3);
								item.arScore[j].ScoreIniInformation.FileSize = info3.Length;
								item.arScore[j].ScoreIniInformation.LastModified = info3.LastWriteTime;
							}

							item.nスコア数++;
							nNbScoresFound++;
						}
						else
						{
							item.arScore[j] = null;
						}
					}
				}

				if (item.nスコア数 > 0)
				{
					listNodeList.Add(item);
					nNbSongNodesFound++;
					if (CDTXMania.ConfigIni.bLogSongSearch)
					{
						StringBuilder builder = new(0x200);
						builder.Append($"nID#{item.nID:D3}");
						if (item.parentNode != null)
						{
							builder.Append($"(in#{item.parentNode.nID:D3}):");
						}
						else
						{
							builder.Append("(onRoot):");
						}

						if (item.strTitle is { Length: > 0 })
						{
							builder.Append(" SONG, Title=" + item.strTitle);
						}

						if (item.strGenre is { Length: > 0 })
						{
							builder.Append(", Genre=" + item.strGenre);
						}

						if (item.col文字色 != Color.White)
						{
							builder.Append(", FontColor=" + item.col文字色);
						}

						Trace.TraceInformation(builder.ToString());
						Trace.Indent();
						try
						{
							for (int k = 0; k < 5; k++)
							{
								if (item.arScore[k] != null)
								{
									CScore cスコア = item.arScore[k];
									builder.Remove(0, builder.Length);
									builder.Append($"ブロック{item.SetDefのブロック番号 + 1}-{k + 1}:");
									builder.Append(" Label=" + item.arDifficultyLabel[k]);
									builder.Append(", File=" + cスコア.FileInformation.AbsoluteFilePath);
									builder.Append(", Size=" + cスコア.FileInformation.FileSize);
									builder.Append(", LastUpdate=" + cスコア.FileInformation.LastModified);
									Trace.TraceInformation(builder.ToString());
								}
							}
						}
						finally
						{
							Trace.Unindent();
						}
					}
				}
			}
		}
		finally
		{
			if (CDTXMania.ConfigIni.bLogSongSearch)
			{
				Trace.Unindent();
			}
		}
	}

	/// <summary>
	/// Append all the set values, if any, of the given <see cref="STHitRanges"/> to the given <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="stHitRanges">The <see cref="STHitRanges"/> to append the values of.</param>
	/// <param name="strName">The unique identifier of <paramref name="stHitRanges"/>.</param>
	/// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
	private void tTryAppendHitRanges(STHitRanges stHitRanges, string strName, StringBuilder builder)
	{
		if (stHitRanges.nPerfectSizeMs >= 0)
			builder.Append($@", {strName}Perfect={stHitRanges.nPerfectSizeMs}ms");

		if (stHitRanges.nGreatSizeMs >= 0)
			builder.Append($@", {strName}Great={stHitRanges.nGreatSizeMs}ms");

		if (stHitRanges.nGoodSizeMs >= 0)
			builder.Append($@", {strName}Good={stHitRanges.nGoodSizeMs}ms");

		if (stHitRanges.nPoorSizeMs >= 0)
			builder.Append($@", {strName}Poor={stHitRanges.nPoorSizeMs}ms");
	}

	//-----------------
	#endregion
	#region [ Reflect score cache in song list ]
	//-----------------
	public void tReflectScoreCacheInSongList()
	{
		nNbScoresFromScoreCache = 0;
		tReflectScoreCacheInSongList( listSongRoot );
	}

	private void tReflectScoreCacheInSongList(List<CSongListNode> nodeList)
	{
		using List<CSongListNode>.Enumerator enumerator = nodeList.GetEnumerator();

		while (enumerator.MoveNext())
		{
			SlowOrSuspendSearchTask(); // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

			CSongListNode node = enumerator.Current;
			if (node.eNodeType == CSongListNode.ENodeType.BOX)
			{
				tReflectScoreCacheInSongList(node.listChildNodes);
			}
			else if ((node.eNodeType == CSongListNode.ENodeType.SCORE) ||
			         (node.eNodeType == CSongListNode.ENodeType.SCORE_MIDI))
			{
				Predicate<CScore> match = null;
				for (int lv = 0; lv < 5; lv++)
				{
					if (node.arScore[lv] != null)
					{
						if (match == null)
						{
							match = delegate(CScore sc)
							{
								return
									(
										(sc.FileInformation.AbsoluteFilePath.Equals(node.arScore[lv].FileInformation
											 .AbsoluteFilePath)
										 && sc.FileInformation.FileSize.Equals(
											 node.arScore[lv].FileInformation.FileSize))
										&& (sc.FileInformation.LastModified.Equals(node.arScore[lv].FileInformation
											    .LastModified)
										    && sc.ScoreIniInformation.FileSize.Equals(node.arScore[lv]
											    .ScoreIniInformation.FileSize)))
									&& sc.ScoreIniInformation.LastModified.Equals(node.arScore[lv].ScoreIniInformation
										.LastModified);
							};
						}

						int nMatched = listSongsDB.FindIndex(match);
						if (nMatched == -1)
						{
//Trace.TraceInformation( "songs.db に存在しません。({0})", node.arScore[ lv ].FileInformation.AbsoluteFilePath );
							if (CDTXMania.ConfigIni.bLogSongSearch)
							{
								Trace.TraceInformation("Not found in songs.db. ({0})",
									node.arScore[lv].FileInformation.AbsoluteFilePath);
							}
						}
						else
						{
							node.arScore[lv].SongInformation = listSongsDB[nMatched].SongInformation;
							node.arScore[lv].bHadACacheInSongDB = true;
							if (CDTXMania.ConfigIni.bLogSongSearch)
							{
								Trace.TraceInformation("Transcribing data from songs.db. ({0})",
									node.arScore[lv].FileInformation.AbsoluteFilePath);
							}

							nNbScoresFromScoreCache++;
							if (node.arScore[lv].ScoreIniInformation.LastModified !=
							    listSongsDB[nMatched].ScoreIniInformation.LastModified)
							{
								string strFileNameScoreIni =
									node.arScore[lv].FileInformation.AbsoluteFilePath + ".score.ini";
								try
								{
									CScoreIni scoreIni = new(strFileNameScoreIni);
									scoreIni.tCheckIntegrity();
									for (int i = 0; i < 3; i++)
									{
										int nSectionHiSkill = (i * 2) + 1;
										int nSectionHiScore = i * 2;
										if (scoreIni.stSection[nSectionHiSkill].bMIDIUsed
										    || scoreIni.stSection[nSectionHiSkill].bKeyboardUsed
										    || scoreIni.stSection[nSectionHiSkill].bJoypadUsed
										    || scoreIni.stSection[nSectionHiSkill].bMouseUsed)
										{
											if (CDTXMania.ConfigIni.nSkillMode == 0)
											{
												node.arScore[lv].SongInformation.BestRank[i] =
													(scoreIni.stFile.BestRank[i] != (int)CScoreIni.ERANK.UNKNOWN)
														? (int)scoreIni.stFile.BestRank[i]
														: CScoreIni.tCalculateRankOld(
															scoreIni.stSection[nSectionHiSkill]);
											}
											else
											{
												node.arScore[lv].SongInformation.BestRank[i] =
													(scoreIni.stFile.BestRank[i] != (int)CScoreIni.ERANK.UNKNOWN)
														? (int)scoreIni.stFile.BestRank[i]
														: CScoreIni.tCalculateRank(scoreIni.stSection[nSectionHiSkill]);
											}
										}
										else
										{
											node.arScore[lv].SongInformation.BestRank[i] = (int)CScoreIni.ERANK.UNKNOWN;
										}

										node.arScore[lv].SongInformation.HighSkill[i] =
											scoreIni.stSection[nSectionHiSkill].dbPerformanceSkill;
										node.arScore[lv].SongInformation.FullCombo[i] =
											scoreIni.stSection[nSectionHiSkill].bIsFullCombo |
											scoreIni.stSection[nSectionHiScore].bIsFullCombo;
									}

									node.arScore[lv].SongInformation.NbPerformances.Drums =
										scoreIni.stFile.PlayCountDrums;
									node.arScore[lv].SongInformation.NbPerformances.Guitar =
										scoreIni.stFile.PlayCountGuitar;
									node.arScore[lv].SongInformation.NbPerformances.Bass =
										scoreIni.stFile.PlayCountBass;
									for (int j = 0; j < 5; j++)
									{
										node.arScore[lv].SongInformation.PerformanceHistory[j] =
											scoreIni.stFile.History[j];
									}

									if (CDTXMania.ConfigIni.bLogSongSearch)
									{
										Trace.TraceInformation(
											"HiSkill information and performance history were retrieved from the performance record file. ({0})",
											strFileNameScoreIni);
									}
								}
								catch
								{
									Trace.TraceError("Failed to read the performance record file. ({0})",
										strFileNameScoreIni);
								}
							}
						}
					}
				}
			}
		}
	}

	private CScore tReadOneScoreFromSongsDB( BinaryReader br )
	{
		CScore cスコア = new();
		cスコア.FileInformation.AbsoluteFilePath = br.ReadString();
		cスコア.FileInformation.AbsoluteFolderPath = br.ReadString();
		cスコア.FileInformation.LastModified = new DateTime( br.ReadInt64() );
		cスコア.FileInformation.FileSize = br.ReadInt64();
		cスコア.ScoreIniInformation.LastModified = new DateTime( br.ReadInt64() );
		cスコア.ScoreIniInformation.FileSize = br.ReadInt64();
		cスコア.SongInformation.Title = br.ReadString();
		cスコア.SongInformation.ArtistName = br.ReadString();
		cスコア.SongInformation.Comment = br.ReadString();
		cスコア.SongInformation.Genre = br.ReadString();
		cスコア.SongInformation.Preimage = br.ReadString();
		cスコア.SongInformation.Premovie = br.ReadString();
		cスコア.SongInformation.Presound = br.ReadString();
		cスコア.SongInformation.Backgound = br.ReadString();
		cスコア.SongInformation.Level.Drums = br.ReadInt32();
		cスコア.SongInformation.Level.Guitar = br.ReadInt32();
		cスコア.SongInformation.Level.Bass = br.ReadInt32();
		cスコア.SongInformation.LevelDec.Drums = br.ReadInt32();
		cスコア.SongInformation.LevelDec.Guitar = br.ReadInt32();
		cスコア.SongInformation.LevelDec.Bass = br.ReadInt32();
		cスコア.SongInformation.BestRank.Drums = br.ReadInt32();
		cスコア.SongInformation.BestRank.Guitar = br.ReadInt32();
		cスコア.SongInformation.BestRank.Bass = br.ReadInt32();
		cスコア.SongInformation.HighSkill.Drums = br.ReadDouble();
		cスコア.SongInformation.HighSkill.Guitar = br.ReadDouble();
		cスコア.SongInformation.HighSkill.Bass = br.ReadDouble();
		cスコア.SongInformation.FullCombo.Drums = br.ReadBoolean();
		cスコア.SongInformation.FullCombo.Guitar = br.ReadBoolean();
		cスコア.SongInformation.FullCombo.Bass = br.ReadBoolean();
		cスコア.SongInformation.NbPerformances.Drums = br.ReadInt32();
		cスコア.SongInformation.NbPerformances.Guitar = br.ReadInt32();
		cスコア.SongInformation.NbPerformances.Bass = br.ReadInt32();
		cスコア.SongInformation.PerformanceHistory.行1 = br.ReadString();
		cスコア.SongInformation.PerformanceHistory.行2 = br.ReadString();
		cスコア.SongInformation.PerformanceHistory.行3 = br.ReadString();
		cスコア.SongInformation.PerformanceHistory.行4 = br.ReadString();
		cスコア.SongInformation.PerformanceHistory.行5 = br.ReadString();
		cスコア.SongInformation.bHiddenLevel = br.ReadBoolean();
		cスコア.SongInformation.b完全にCLASSIC譜面である.Drums = br.ReadBoolean();
		cスコア.SongInformation.b完全にCLASSIC譜面である.Guitar = br.ReadBoolean();
		cスコア.SongInformation.b完全にCLASSIC譜面である.Bass = br.ReadBoolean();
		cスコア.SongInformation.bScoreExists.Drums = br.ReadBoolean();
		cスコア.SongInformation.bScoreExists.Guitar = br.ReadBoolean();
		cスコア.SongInformation.bScoreExists.Bass = br.ReadBoolean();
		cスコア.SongInformation.SongType = (CDTX.EType) br.ReadInt32();
		cスコア.SongInformation.Bpm = br.ReadDouble();
		cスコア.SongInformation.Duration = br.ReadInt32();
		//Read Progress after Duration
		cスコア.SongInformation.progress.Drums = br.ReadString();
		cスコア.SongInformation.progress.Guitar = br.ReadString();
		cスコア.SongInformation.progress.Bass = br.ReadString();
			
		//
		cスコア.SongInformation.chipCountByInstrument.Drums = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.LC] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.HH] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.SD] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.LP] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.HT] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BD] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.LT] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.FT] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.CY] = br.ReadInt32();

		cスコア.SongInformation.chipCountByInstrument.Guitar = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.GtR] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.GtG] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.GtB] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.GtY] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.GtP] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.GtPick] = br.ReadInt32();

		cスコア.SongInformation.chipCountByInstrument.Bass = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BsR] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BsG] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BsB] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BsY] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BsP] = br.ReadInt32();
		cスコア.SongInformation.chipCountByLane[ELane.BsPick] = br.ReadInt32();

		//Debug.WriteLine( "songs.db: " + cスコア.FileInformation.AbsoluteFilePath );
		return cスコア;
	}
	//-----------------
	#endregion
	#region [ Process song data that wasn't found inside songs.db ]

	public int ProcessSongDataProgress { get; private set; } = 0;
	public int ProcessSongDataTotal { get; private set; } = 0;
	public string ProcessSongDataPath { get; private set; } = "";
	//-----------------
	public async Task PrepareProcessNewSongData()
	{
		List<Task> taskList = [];
		DateTime startTime = DateTime.Now;
			
		SlowOrSuspendSearchTask();
			
		PrepareProcessNewSongData(listSongRoot, ref taskList);
			
		//run the tasks across worker threads
		//check cpu count and use that many threads
		int workerThreads = Environment.ProcessorCount;

		workerThreads = Math.Max(2, workerThreads - 4); //leave some cpu for the game
			
		//Console.WriteLine("WARNING: Worker threads limited to 1 to debug!!!");
		//workerThreads = 1;
			
		Console.WriteLine("Song processing worker threads: " + workerThreads);

		int totalTasks = taskList.Count;
			
		ProcessSongDataProgress = 0;
		ProcessSongDataTotal = totalTasks;
		ProcessSongDataPath = "";
			
		int[] progress = new int[workerThreads];
			
		Thread[] threads = new Thread[workerThreads];
			
		for (int i = 0; i < workerThreads; i++)
		{
			int threadIndex = i;
				
			int perThread = totalTasks / workerThreads;
			List<Task> threadTaskList = taskList.Take(perThread).ToList();
			taskList.RemoveRange(0, perThread);
				
			if (i == workerThreads - 1)
			{
				threadTaskList.AddRange(taskList);
			}
				
			threads[i] = new Thread(() =>
			{
				while (threadTaskList.Count > 0)
				{
					Task task = threadTaskList[0];
					threadTaskList.RemoveAt(0);
						
					task.Start();
					task.Wait();
					progress[threadIndex]++;
				}
			});
		}
			
		//start all threads
		foreach (Thread thread in threads)
		{
			thread.Start();
		}
			
		//wait for all threads to finish
		while (threads.Any(thread => thread.IsAlive))
		{
			//get total progress
			ProcessSongDataProgress = progress.Sum();
			await Task.Delay(100);
		}
			
		Console.WriteLine($"Processed {progress.Sum()} songs in {DateTime.Now - startTime}");
	}
		
	public void PrepareProcessNewSongData(List<CSongListNode> nodeList, ref List<Task> list)
	{
		foreach ( CSongListNode songListNode in nodeList )
		{
			switch (songListNode.eNodeType)
			{
				case CSongListNode.ENodeType.BOX:
					PrepareProcessNewSongData(songListNode.listChildNodes, ref list);
					break;
		            
				case CSongListNode.ENodeType.SCORE or CSongListNode.ENodeType.SCORE_MIDI:
				{
					Task task = new(() =>
					{
						for (int i = 0; i < 5; i++)
						{
							if (songListNode.arScore[i] != null && !songListNode.arScore[i].bHadACacheInSongDB)
							{
								ProcessSongDataPath = songListNode.arScore[i].FileInformation.AbsoluteFilePath;
								ProcessListNode(songListNode, ref songListNode.arScore[i]);
							}
						}
					});
					list.Add(task);
					break;
				}
			}
		}
	}

	private void ProcessListNode(CSongListNode node, ref CScore score)
	{
		string path = score.FileInformation.AbsoluteFilePath;
			
		if (File.Exists(path))
		{
			try
			{
				CDTX cdtx = new(score.FileInformation.AbsoluteFilePath, false);    //2013.06.04 kairera0467 ここの「ヘッダのみ読み込む」をfalseにすると、選曲画面のBPM表示が狂う場合があるので注意。
				//CDTX cdtx2 = new CDTX( c曲リストノード.arScore[ i ].FileInformation.AbsoluteFilePath, false );
				score.SongInformation.Title = cdtx.TITLE;
				score.SongInformation.ArtistName = cdtx.ARTIST;
				score.SongInformation.Comment = cdtx.COMMENT;
				score.SongInformation.Genre = cdtx.GENRE;
				score.SongInformation.Preimage = cdtx.PREIMAGE;
				score.SongInformation.Premovie = cdtx.PREMOVIE;
				score.SongInformation.Presound = cdtx.PREVIEW;
				score.SongInformation.Backgound = cdtx.BACKGROUND is { Length: > 0 } ? cdtx.BACKGROUND : cdtx.BACKGROUND_GR;
				score.SongInformation.Level.Drums = cdtx.LEVEL.Drums;
				score.SongInformation.Level.Guitar = cdtx.LEVEL.Guitar;
				score.SongInformation.Level.Bass = cdtx.LEVEL.Bass;
				score.SongInformation.LevelDec.Drums = cdtx.LEVELDEC.Drums;
				score.SongInformation.LevelDec.Guitar = cdtx.LEVELDEC.Guitar;
				score.SongInformation.LevelDec.Bass = cdtx.LEVELDEC.Bass;
				score.SongInformation.bHiddenLevel = cdtx.HIDDENLEVEL;
				score.SongInformation.b完全にCLASSIC譜面である.Drums = cdtx.bHasChips is { LeftCymbal: false, LP: false, LBD: false, FT: false, Ride: false };
				score.SongInformation.b完全にCLASSIC譜面である.Guitar = !cdtx.bHasChips.YPGuitar;
				score.SongInformation.b完全にCLASSIC譜面である.Bass = !cdtx.bHasChips.YPBass;
				score.SongInformation.bScoreExists.Drums = cdtx.bHasChips.Drums;
				score.SongInformation.bScoreExists.Guitar = cdtx.bHasChips.Guitar;
				score.SongInformation.bScoreExists.Bass = cdtx.bHasChips.Bass;
				score.SongInformation.SongType = cdtx.eFileType;
				score.SongInformation.Bpm = cdtx.BPM;
				score.SongInformation.Duration = (cdtx.listChip == null) ? 0 : cdtx.listChip[cdtx.listChip.Count - 1].nPlaybackTimeMs;
					
				score.SongInformation.chipCountByInstrument.Drums = cdtx.nVisibleChipsCount.Drums;
				{
					score.SongInformation.chipCountByLane[ELane.LC] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.LC);
					score.SongInformation.chipCountByLane[ELane.HH] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.HH);
					score.SongInformation.chipCountByLane[ELane.SD] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.SD);
					score.SongInformation.chipCountByLane[ELane.LP] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.LP);
					score.SongInformation.chipCountByLane[ELane.HT] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.HT);
					score.SongInformation.chipCountByLane[ELane.BD] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BD);
					score.SongInformation.chipCountByLane[ELane.LT] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.LT);
					score.SongInformation.chipCountByLane[ELane.FT] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.FT);
					score.SongInformation.chipCountByLane[ELane.CY] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.CY);
				}

				score.SongInformation.chipCountByInstrument.Guitar = cdtx.nVisibleChipsCount.Guitar;
				{
					score.SongInformation.chipCountByLane[ELane.GtR] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtR);
					score.SongInformation.chipCountByLane[ELane.GtG] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtG);
					score.SongInformation.chipCountByLane[ELane.GtB] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtB);
					score.SongInformation.chipCountByLane[ELane.GtY] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtY);
					score.SongInformation.chipCountByLane[ELane.GtP] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtP);
					score.SongInformation.chipCountByLane[ELane.GtPick] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtPick);
				}

				score.SongInformation.chipCountByInstrument.Bass = cdtx.nVisibleChipsCount.Bass;
				{
					score.SongInformation.chipCountByLane[ELane.BsR] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsR);
					score.SongInformation.chipCountByLane[ELane.BsG] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsG);
					score.SongInformation.chipCountByLane[ELane.BsB] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsB);
					score.SongInformation.chipCountByLane[ELane.BsY] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsY);
					score.SongInformation.chipCountByLane[ELane.BsP] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsP);
					score.SongInformation.chipCountByLane[ELane.BsPick] = cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsPick);
				}

				nNbScoresFromFile++;
				cdtx.OnDeactivate();
				//Debug.WriteLine( "★" + this.nNbScoresFromFile + " " + c曲リストノード.arScore[ i ].SongInformation.Title );
				#region [ 曲検索ログ出力 ]
				//-----------------
				if (CDTXMania.ConfigIni.bLogSongSearch)
				{
					StringBuilder sb = new(0x400);
					sb.Append($"曲データファイルから譜面情報を転記しました。({path})");
					sb.Append("(title=" + score.SongInformation.Title);
					sb.Append(", artist=" + score.SongInformation.ArtistName);
					sb.Append(", comment=" + score.SongInformation.Comment);
					sb.Append(", genre=" + score.SongInformation.Genre);
					sb.Append(", preimage=" + score.SongInformation.Preimage);
					sb.Append(", premovie=" + score.SongInformation.Premovie);
					sb.Append(", presound=" + score.SongInformation.Presound);
					sb.Append(", background=" + score.SongInformation.Backgound);
					sb.Append(", lvDr=" + score.SongInformation.Level.Drums);
					sb.Append(", lvGt=" + score.SongInformation.Level.Guitar);
					sb.Append(", lvBs=" + score.SongInformation.Level.Bass);
					sb.Append(", lvHide=" + score.SongInformation.bHiddenLevel);
					sb.Append(", classic=" + score.SongInformation.b完全にCLASSIC譜面である);
					sb.Append(", type=" + score.SongInformation.SongType);
					sb.Append(", bpm=" + score.SongInformation.Bpm);
					//	sb.Append( ", duration=" + c曲リストノード.arScore[ i ].SongInformation.Duration );
					Trace.TraceInformation(sb.ToString());
				}
				//-----------------
				#endregion
			}
			catch (Exception exception)
			{
				Trace.TraceError(exception.Message);
				score = null;
				node.nスコア数--;
				nNbScoresFound--;
				Trace.TraceError("曲データファイルの読み込みに失敗しました。({0})", path);
				return;
			}
		}
			
		tReadScoreIniAndSetScoreInformation(score.FileInformation.AbsoluteFilePath + ".score.ini", ref score);
	}

	//-----------------
	#endregion
	#region [ 曲リストへ後処理を適用する ]
	//-----------------
	public void t曲リストへ後処理を適用する()
	{
		listStrBoxDefSkinSubfolderFullName = new List<string>();
		if ( CDTXMania.Skin.strBoxDefSkinSubfolders != null )
		{
			foreach ( string b in CDTXMania.Skin.strBoxDefSkinSubfolders )
			{
				listStrBoxDefSkinSubfolderFullName.Add( b );
			}
		}

		t曲リストへ後処理を適用する( listSongRoot );

		#region [ skin名で比較して、systemスキンとboxdefスキンに重複があれば、boxdefスキン側を削除する ]
		string[] systemSkinNames = CSkin.GetSkinName( CDTXMania.Skin.strSystemSkinSubfolders );
		List<string> l = new List<string>( listStrBoxDefSkinSubfolderFullName );
		foreach ( string boxdefSkinSubfolderFullName in l )
		{
			if ( Array.BinarySearch( systemSkinNames,
				    CSkin.GetSkinName( boxdefSkinSubfolderFullName ),
				    StringComparer.InvariantCultureIgnoreCase ) >= 0 )
			{
				listStrBoxDefSkinSubfolderFullName.Remove( boxdefSkinSubfolderFullName );
			}
		}
		#endregion
		string[] ba = listStrBoxDefSkinSubfolderFullName.ToArray();
		Array.Sort( ba );
		CDTXMania.Skin.strBoxDefSkinSubfolders = ba;
	}
	private void t曲リストへ後処理を適用する( List<CSongListNode> ノードリスト )
	{
		#region [ リストに１つ以上の曲があるなら RANDOM BOX を入れる ]
		//-----------------------------
		if( ノードリスト.Count > 0 )
		{
			CSongListNode itemRandom = new()
			{
				eNodeType = CSongListNode.ENodeType.RANDOM,
				strTitle = "< RANDOM SELECT >",
				nスコア数 = 5,
				parentNode = ノードリスト[ 0 ].parentNode
			};

			itemRandom.strBreadcrumbs = ( itemRandom.parentNode == null ) ?
				itemRandom.strTitle :  itemRandom.parentNode.strBreadcrumbs + " > " + itemRandom.strTitle;

			for( int i = 0; i < 5; i++ )
			{
				itemRandom.arScore[ i ] = new CScore();
				itemRandom.arScore[ i ].SongInformation.Title = string.Format( "< RANDOM SELECT Lv.{0} >", i + 1 );
				itemRandom.arScore[ i ].SongInformation.Preimage = CSkin.Path(@"Graphics\5_preimage random.png");
				itemRandom.arScore[ i ].SongInformation.Comment =
					CDTXMania.isJapanese ? 
						string.Format("難易度レベル {0} 付近の曲をランダムに選択します。難易度レベルを持たない曲も選択候補となります。", i + 1) : 
						string.Format("Random select from the songs which has the level about L{0}. Non-leveled songs may also selected.", i + 1);
				itemRandom.arDifficultyLabel[ i ] = string.Format( "L{0}", i + 1 );
			}
			ノードリスト.Add( itemRandom );

			#region [ ログ出力 ]
			//-----------------------------
			if( CDTXMania.ConfigIni.bLogSongSearch )
			{
				StringBuilder sb = new StringBuilder( 0x100 );
				sb.Append( string.Format( "nID#{0:D3}", itemRandom.nID ) );
				if( itemRandom.parentNode != null )
				{
					sb.Append( string.Format( "(in#{0:D3}):", itemRandom.parentNode.nID ) );
				}
				else
				{
					sb.Append( "(onRoot):" );
				}
				sb.Append( " RANDOM" );
				Trace.TraceInformation( sb.ToString() );
			}
			//-----------------------------
			#endregion
		}
		//-----------------------------
		#endregion

		// すべてのノードについて…
		foreach( CSongListNode c曲リストノード in ノードリスト )
		{
			SlowOrSuspendSearchTask();		// #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

			#region [ BOXノードなら子リストに <<BACK を入れ、子リストに後処理を適用する ]
			//-----------------------------
			if( c曲リストノード.eNodeType == CSongListNode.ENodeType.BOX )
			{
				if (c曲リストノード.strSkinPath != "" && !listStrBoxDefSkinSubfolderFullName.Contains(c曲リストノード.strSkinPath))
				{
					listStrBoxDefSkinSubfolderFullName.Add(c曲リストノード.strSkinPath);
				}

				CSongListNode itemBack = new()
				{
					eNodeType = CSongListNode.ENodeType.BACKBOX,
					strTitle = "<< BACK",
					nスコア数 = 1,
					parentNode = c曲リストノード,
					strSkinPath = ( c曲リストノード.parentNode == null ) ?
						"" : c曲リストノード.parentNode.strSkinPath
				};

				itemBack.strBreadcrumbs = ( itemBack.parentNode == null ) ?
					itemBack.strTitle : itemBack.parentNode.strBreadcrumbs + " > " + itemBack.strTitle;

				itemBack.arScore[ 0 ] = new CScore();
				itemBack.arScore[ 0 ].FileInformation.AbsoluteFolderPath = "";
				itemBack.arScore[ 0 ].SongInformation.Title = itemBack.strTitle;
				itemBack.arScore[ 0 ].SongInformation.Preimage = CSkin.Path(@"Graphics\5_preimage backbox.png");
				itemBack.arScore[ 0 ].SongInformation.Comment =
					CDTXMania.isJapanese ?
						"BOX を出ます。" :
						"Exit from the BOX.";
				c曲リストノード.listChildNodes.Insert( 0, itemBack );

				#region [ ログ出力 ]
				//-----------------------------
				if( CDTXMania.ConfigIni.bLogSongSearch )
				{
					StringBuilder sb = new( 0x100 );
					sb.Append($"nID#{itemBack.nID:D3}");
					if( itemBack.parentNode != null )
					{
						sb.Append($"(in#{itemBack.parentNode.nID:D3}):");
					}
					else
					{
						sb.Append( "(onRoot):" );
					}
					sb.Append( " BACKBOX" );
					Trace.TraceInformation( sb.ToString() );
				}
				//-----------------------------
				#endregion

				t曲リストへ後処理を適用する( c曲リストノード.listChildNodes );
				continue;
			}
			//-----------------------------
			#endregion

			#region [ ノードにタイトルがないなら、最初に見つけたスコアのタイトルを設定する ]
			//-----------------------------
			if( string.IsNullOrEmpty( c曲リストノード.strTitle ) )
			{
				for( int j = 0; j < 5; j++ )
				{
					if( ( c曲リストノード.arScore[ j ] != null ) && !string.IsNullOrEmpty( c曲リストノード.arScore[ j ].SongInformation.Title ) )
					{
						c曲リストノード.strTitle = c曲リストノード.arScore[ j ].SongInformation.Title;

						if( CDTXMania.ConfigIni.bLogSongSearch )
							Trace.TraceInformation( "タイトルを設定しました。(nID#{0:D3}, title={1})", c曲リストノード.nID, c曲リストノード.strTitle );

						break;
					}
				}
			}
			//-----------------------------
			#endregion
		}

		#region [ ノードをソートする ]
		//-----------------------------
		t曲リストのソート1_絶対パス順( ノードリスト );
		//-----------------------------
		#endregion
	}
	//-----------------
	#endregion
	#region [ スコアキャッシュをSongsDBに出力する ]
	//-----------------
	public void tスコアキャッシュをSongsDBに出力する( string SongsDBファイル名 )
	{
		nNbScoresForSongsDB = 0;
		try
		{
			BinaryWriter bw = new( new FileStream( SongsDBファイル名, FileMode.Create, FileAccess.Write ) );
			bw.Write( SONGSDB_VERSION );
			tSongsDBにリストを１つ出力する( bw, listSongRoot );
			bw.Close();
		}
		catch
		{
			Trace.TraceError( "songs.dbの出力に失敗しました。" );
		}
	}
	private void tSongsDBにノードを１つ出力する( BinaryWriter bw, CSongListNode node )
	{
		for( int i = 0; i < 5; i++ )
		{
			// ここではsuspendに応じないようにしておく(深い意味はない。ファイルの書き込みオープン状態を長時間維持したくないだけ)
			//if ( this.bIsSuspending )		// #27060 中断要求があったら、解除要求が来るまで待機
			//{
			//	autoReset.WaitOne();
			//}

			if( node.arScore[ i ] != null )
			{
				bw.Write( node.arScore[ i ].FileInformation.AbsoluteFilePath );
				bw.Write( node.arScore[ i ].FileInformation.AbsoluteFolderPath );
				bw.Write( node.arScore[ i ].FileInformation.LastModified.Ticks );
				bw.Write( node.arScore[ i ].FileInformation.FileSize );
				bw.Write( node.arScore[ i ].ScoreIniInformation.LastModified.Ticks );
				bw.Write( node.arScore[ i ].ScoreIniInformation.FileSize );
				bw.Write( node.arScore[ i ].SongInformation.Title );
				bw.Write( node.arScore[ i ].SongInformation.ArtistName );
				bw.Write( node.arScore[ i ].SongInformation.Comment );
				bw.Write( node.arScore[ i ].SongInformation.Genre );
				bw.Write( node.arScore[ i ].SongInformation.Preimage );
				bw.Write( node.arScore[ i ].SongInformation.Premovie );
				bw.Write( node.arScore[ i ].SongInformation.Presound );
				bw.Write( node.arScore[ i ].SongInformation.Backgound );
				bw.Write( node.arScore[ i ].SongInformation.Level.Drums );
				bw.Write( node.arScore[ i ].SongInformation.Level.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.Level.Bass );
				bw.Write( node.arScore[ i ].SongInformation.LevelDec.Drums );
				bw.Write( node.arScore[ i ].SongInformation.LevelDec.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.LevelDec.Bass );
				bw.Write( node.arScore[ i ].SongInformation.BestRank.Drums );
				bw.Write( node.arScore[ i ].SongInformation.BestRank.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.BestRank.Bass );
				bw.Write( node.arScore[ i ].SongInformation.HighSkill.Drums );
				bw.Write( node.arScore[ i ].SongInformation.HighSkill.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.HighSkill.Bass );
				bw.Write( node.arScore[ i ].SongInformation.FullCombo.Drums );
				bw.Write( node.arScore[ i ].SongInformation.FullCombo.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.FullCombo.Bass );
				bw.Write( node.arScore[ i ].SongInformation.NbPerformances.Drums );
				bw.Write( node.arScore[ i ].SongInformation.NbPerformances.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.NbPerformances.Bass );
				bw.Write( node.arScore[ i ].SongInformation.PerformanceHistory.行1 );
				bw.Write( node.arScore[ i ].SongInformation.PerformanceHistory.行2 );
				bw.Write( node.arScore[ i ].SongInformation.PerformanceHistory.行3 );
				bw.Write( node.arScore[ i ].SongInformation.PerformanceHistory.行4 );
				bw.Write( node.arScore[ i ].SongInformation.PerformanceHistory.行5 );
				bw.Write( node.arScore[ i ].SongInformation.bHiddenLevel );
				bw.Write( node.arScore[ i ].SongInformation.b完全にCLASSIC譜面である.Drums );
				bw.Write( node.arScore[ i ].SongInformation.b完全にCLASSIC譜面である.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.b完全にCLASSIC譜面である.Bass );
				bw.Write( node.arScore[ i ].SongInformation.bScoreExists.Drums );
				bw.Write( node.arScore[ i ].SongInformation.bScoreExists.Guitar );
				bw.Write( node.arScore[ i ].SongInformation.bScoreExists.Bass );
				bw.Write( (int) node.arScore[ i ].SongInformation.SongType );
				bw.Write( node.arScore[ i ].SongInformation.Bpm );
				bw.Write( node.arScore[ i ].SongInformation.Duration );
				//Write to Progress here
				bw.Write(node.arScore[i].SongInformation.progress.Drums);
				bw.Write(node.arScore[i].SongInformation.progress.Guitar);
				bw.Write(node.arScore[i].SongInformation.progress.Bass);

				//New Data: Chip counts for Instrument and Lanes
				bw.Write(node.arScore[i].SongInformation.chipCountByInstrument.Drums);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.LC]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.HH]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.SD]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.LP]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.HT]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BD]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.LT]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.FT]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.CY]);

				bw.Write(node.arScore[i].SongInformation.chipCountByInstrument.Guitar);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.GtR]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.GtG]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.GtB]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.GtY]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.GtP]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.GtPick]);

				bw.Write(node.arScore[i].SongInformation.chipCountByInstrument.Bass);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BsR]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BsG]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BsB]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BsY]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BsP]);
				bw.Write(node.arScore[i].SongInformation.chipCountByLane[ELane.BsPick]);

				nNbScoresForSongsDB++;
			}
		}
	}
	private void tSongsDBにリストを１つ出力する( BinaryWriter bw, List<CSongListNode> list )
	{
		foreach( CSongListNode c曲リストノード in list )
		{
			if(    ( c曲リストノード.eNodeType == CSongListNode.ENodeType.SCORE )
			       || ( c曲リストノード.eNodeType == CSongListNode.ENodeType.SCORE_MIDI ) )
			{
				tSongsDBにノードを１つ出力する( bw, c曲リストノード );
			}
			if( c曲リストノード.listChildNodes != null )
			{
				tSongsDBにリストを１つ出力する( bw, c曲リストノード.listChildNodes );
			}
		}
	}
	//-----------------
	#endregion
		
	#region [ 曲リストソート ]
	//-----------------
	public void t曲リストのソート1_絶対パス順( List<CSongListNode> ノードリスト )
	{
		ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
		{
			#region [ 共通処理 ]
			if ( n1 == n2 )
			{
				return 0;
			}
			int num = t比較0_共通( n1, n2 );
			if( num != 0 )
			{
				return num;
			}
			if( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
			{
				return n1.arScore[ 0 ].FileInformation.AbsoluteFolderPath.CompareTo( n2.arScore[ 0 ].FileInformation.AbsoluteFolderPath );
			}
			#endregion
			string str = "";
			if( string.IsNullOrEmpty( n1.pathSetDefの絶対パス ) )
			{
				for( int i = 0; i < 5; i++ )
				{
					if( n1.arScore[ i ] != null )
					{
						str = n1.arScore[ i ].FileInformation.AbsoluteFilePath;
						if( str == null )
						{
							str = "";
						}
						break;
					}
				}
			}
			else
			{
				str = n1.pathSetDefの絶対パス + n1.SetDefのブロック番号.ToString( "00" );
			}
			string strB = "";
			if( string.IsNullOrEmpty( n2.pathSetDefの絶対パス ) )
			{
				for( int j = 0; j < 5; j++ )
				{
					if( n2.arScore[ j ] != null )
					{
						strB = n2.arScore[ j ].FileInformation.AbsoluteFilePath;
						if( strB == null )
						{
							strB = "";
						}
						break;
					}
				}
			}
			else
			{
				strB = n2.pathSetDefの絶対パス + n2.SetDefのブロック番号.ToString( "00" );
			}
			return str.CompareTo( strB );
		} );
		foreach( CSongListNode c曲リストノード in ノードリスト )
		{
			if( ( c曲リストノード.listChildNodes != null ) && ( c曲リストノード.listChildNodes.Count > 1 ) )
			{
				t曲リストのソート1_絶対パス順( c曲リストノード.listChildNodes );
			}
		}
	}
	public void t曲リストのソート2_タイトル順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
		{
			if( n1 == n2 )
			{
				return 0;
			}
			int num = t比較0_共通( n1, n2 );
			if( num != 0 )
			{
				return order * num;
			}
			return order * n1.strTitle.CompareTo( n2.strTitle );
		} );
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="ノードリスト"></param>
	/// <param name="part"></param>
	/// <param name="order">1=Ascend -1=Descend</param>
	public void t曲リストのソート3_演奏回数の多い順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		order = -order;
		int nL12345 = (int) p[ 0 ];
		if ( part != EInstrumentPart.UNKNOWN )
		{
			ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
			{
				#region [ 共通処理 ]
				if ( n1 == n2 )
				{
					return 0;
				}
				int num = t比較0_共通( n1, n2 );
				if( num != 0 )
				{
					return order * num;
				}
				if( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
				{
					return order * n1.arScore[ 0 ].FileInformation.AbsoluteFolderPath.CompareTo( n2.arScore[ 0 ].FileInformation.AbsoluteFolderPath );
				}
				#endregion
				int nSumPlayCountN1 = 0, nSumPlayCountN2 = 0;
//					for( int i = 0; i < 5; i++ )
//					{
				if( n1.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 += n1.arScore[ nL12345 ].SongInformation.NbPerformances[ (int) part ];
				}
				if( n2.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN2 += n2.arScore[ nL12345 ].SongInformation.NbPerformances[ (int) part ];
				}
//					}
				num = nSumPlayCountN2 - nSumPlayCountN1;
				if( num != 0 )
				{
					return order * num;
				}
				return order * n1.strTitle.CompareTo( n2.strTitle );
			} );
			foreach ( CSongListNode c曲リストノード in ノードリスト )
			{
				int nSumPlayCountN1 = 0;
//					for ( int i = 0; i < 5; i++ )
//					{
				if ( c曲リストノード.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 += c曲リストノード.arScore[ nL12345 ].SongInformation.NbPerformances[ (int) part ];
				}
//					}
// Debug.WriteLine( nSumPlayCountN1 + ":" + c曲リストノード.strタイトル );
			}

//				foreach( CSongListNode c曲リストノード in ノードリスト )
//				{
//					if( ( c曲リストノード.list子リスト != null ) && ( c曲リストノード.list子リスト.Count > 1 ) )
//					{
//						this.t曲リストのソート3_演奏回数の多い順( c曲リストノード.list子リスト, part );
//					}
//				}
		}
	}
	public void t曲リストのソート4_LEVEL順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		order = -order;
		int nL12345 = (int)p[ 0 ];
		if ( part != EInstrumentPart.UNKNOWN )
		{
			Trace.WriteLine( "----------ソート開始------------" );
			ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 ) //2016.03.12 kairera0467 少数第2位も考慮するようにするテスト。
			{
				#region [ 共通処理 ]
				if ( n1 == n2 )
				{
					return 0;
				}
				float num = t比較0_共通( n1, n2 ); //2016.06.17 kairera0467 ソートが正確に行われるよう修正。(int→float)
				if ( num != 0 )
				{
					return (int)(order * num);
				}
				if ( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
				{
					return order * n1.arScore[ 0 ].FileInformation.AbsoluteFolderPath.CompareTo( n2.arScore[ 0 ].FileInformation.AbsoluteFolderPath );
				}
				#endregion
				float nSumPlayCountN1 = 0, nSumPlayCountN2 = 0;
				if ( n1.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = ( n1.arScore[ nL12345 ].SongInformation.Level[ (int) part ] / 10.0f ) + ( n1.arScore[ nL12345 ].SongInformation.LevelDec[ (int) part ] / 100.0f );
				}
				if ( n2.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN2 = ( n2.arScore[ nL12345 ].SongInformation.Level[ (int) part ] / 10.0f ) + ( n2.arScore[ nL12345 ].SongInformation.LevelDec[ (int) part ] / 100.0f );
				}
				num = nSumPlayCountN2 - nSumPlayCountN1;
				if ( num != 0 )
				{
					return (int)( (order * num) * 100 );
				}
				return order * n1.strTitle.CompareTo( n2.strTitle );
			} );
			foreach ( CSongListNode c曲リストノード in ノードリスト )
			{
				int nSumPlayCountN1 = 0;
				if ( c曲リストノード.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = c曲リストノード.arScore[ nL12345 ].SongInformation.Level[ (int) part ] + c曲リストノード.arScore[ nL12345 ].SongInformation.LevelDec[ (int) part ];
				}
// Debug.WriteLine( nSumPlayCountN1 + ":" + c曲リストノード.strタイトル );
			}
		}
	}
	public void t曲リストのソート5_BestRank順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		order = -order;
		int nL12345 = (int) p[ 0 ];
		if ( part != EInstrumentPart.UNKNOWN )
		{
			ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
			{
				#region [ 共通処理 ]
				if ( n1 == n2 )
				{
					return 0;
				}
				int num = t比較0_共通( n1, n2 );
				if ( num != 0 )
				{
					return order * num;
				}
				if ( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
				{
					return order * n1.arScore[ 0 ].FileInformation.AbsoluteFolderPath.CompareTo( n2.arScore[ 0 ].FileInformation.AbsoluteFolderPath );
				}
				#endregion
				int nSumPlayCountN1 = 0, nSumPlayCountN2 = 0;
				bool isFullCombo1 = false, isFullCombo2 = false;
				if ( n1.arScore[ nL12345 ] != null )
				{
					isFullCombo1 = n1.arScore[ nL12345 ].SongInformation.FullCombo[ (int) part ];
					nSumPlayCountN1 = n1.arScore[ nL12345 ].SongInformation.BestRank[ (int) part ];
				}
				if ( n2.arScore[ nL12345 ] != null )
				{
					isFullCombo2 = n2.arScore[ nL12345 ].SongInformation.FullCombo[ (int) part ];
					nSumPlayCountN2 = n2.arScore[ nL12345 ].SongInformation.BestRank[ (int) part ];
				}
				if ( isFullCombo1 ^ isFullCombo2 )
				{
					if ( isFullCombo1 ) return order; else return -order;
				}
				num = nSumPlayCountN2 - nSumPlayCountN1;
				if ( num != 0 )
				{
					return order * num;
				}
				return order * n1.strTitle.CompareTo( n2.strTitle );
			} );
			foreach ( CSongListNode c曲リストノード in ノードリスト )
			{
				int nSumPlayCountN1 = 0;
				if ( c曲リストノード.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = c曲リストノード.arScore[ nL12345 ].SongInformation.BestRank[ (int) part ];
				}
// Debug.WriteLine( nSumPlayCountN1 + ":" + c曲リストノード.strタイトル );
			}
		}
	}
	public void t曲リストのソート6_SkillPoint順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		order = -order;
		int nL12345 = (int) p[ 0 ];
		if ( part != EInstrumentPart.UNKNOWN )
		{
			ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
			{
				#region [ 共通処理 ]
				if ( n1 == n2 )
				{
					return 0;
				}
				int num = t比較0_共通( n1, n2 );
				if ( num != 0 )
				{
					return order * num;
				}
				if ( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
				{
					return order * n1.arScore[ 0 ].FileInformation.AbsoluteFolderPath.CompareTo( n2.arScore[ 0 ].FileInformation.AbsoluteFolderPath );
				}
				#endregion
				double nSumPlayCountN1 = 0, nSumPlayCountN2 = 0;
				if ( n1.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = n1.arScore[ nL12345 ].SongInformation.HighSkill[ (int) part ];
				}
				if ( n2.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN2 = n2.arScore[ nL12345 ].SongInformation.HighSkill[ (int) part ];
				}
				double d = nSumPlayCountN2 - nSumPlayCountN1;
				if ( d != 0 )
				{
					return order * Math.Sign(d);
				}
				return order * n1.strTitle.CompareTo( n2.strTitle );
			} );
			foreach ( CSongListNode c曲リストノード in ノードリスト )
			{
				double nSumPlayCountN1 = 0;
				if ( c曲リストノード.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = c曲リストノード.arScore[ nL12345 ].SongInformation.HighSkill[ (int) part ];
				}
// Debug.WriteLine( nSumPlayCountN1 + ":" + c曲リストノード.strタイトル );
			}
		}
	}
	public void t曲リストのソート7_更新日時順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		int nL12345 = (int) p[ 0 ];
		if ( part != EInstrumentPart.UNKNOWN )
		{
			ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
			{
				#region [ 共通処理 ]
				if ( n1 == n2 )
				{
					return 0;
				}
				int num = t比較0_共通( n1, n2 );
				if ( num != 0 )
				{
					return order * num;
				}
				if ( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
				{
					return order * n1.arScore[ 0 ].FileInformation.AbsoluteFolderPath.CompareTo( n2.arScore[ 0 ].FileInformation.AbsoluteFolderPath );
				}
				#endregion
				DateTime nSumPlayCountN1 = DateTime.Parse("0001/01/01 12:00:01.000");
				DateTime nSumPlayCountN2 = DateTime.Parse("0001/01/01 12:00:01.000");
				if ( n1.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = n1.arScore[ nL12345 ].FileInformation.LastModified;
				}
				if ( n2.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN2 = n2.arScore[ nL12345 ].FileInformation.LastModified;
				}
				int d = nSumPlayCountN1.CompareTo(nSumPlayCountN2);
				if ( d != 0 )
				{
					return order * Math.Sign( d );
				}
				return order * n1.strTitle.CompareTo( n2.strTitle );
			} );
			foreach ( CSongListNode c曲リストノード in ノードリスト )
			{
				DateTime nSumPlayCountN1 = DateTime.Parse( "0001/01/01 12:00:01.000" );
				if ( c曲リストノード.arScore[ nL12345 ] != null )
				{
					nSumPlayCountN1 = c曲リストノード.arScore[ nL12345 ].FileInformation.LastModified;
				}
// Debug.WriteLine( nSumPlayCountN1 + ":" + c曲リストノード.strタイトル );
			}
		}
	}
	public void t曲リストのソート8_アーティスト名順( List<CSongListNode> ノードリスト, EInstrumentPart part, int order, params object[] p )
	{
		int nL12345 = (int) p[ 0 ]; 
		ノードリスト.Sort( delegate( CSongListNode n1, CSongListNode n2 )
		{
			if ( n1 == n2 )
			{
				return 0;
			}
			int num = t比較0_共通( n1, n2 );
			if ( num != 0 )
			{
				return order * Math.Sign( num );
			}
			string strAuthorN1 = "";
			string strAuthorN2 = "";
			if (n1.arScore[ nL12345 ] != null )
			{
				strAuthorN1 = n1.arScore[ nL12345 ].SongInformation.ArtistName;
			}
			if ( n2.arScore[ nL12345 ] != null )
			{
				strAuthorN2 = n2.arScore[ nL12345 ].SongInformation.ArtistName;
			}

			return order * strAuthorN1.CompareTo( strAuthorN2 );
		} );
		foreach ( CSongListNode c曲リストノード in ノードリスト )
		{
			string s = "";
			if ( c曲リストノード.arScore[ nL12345 ] != null )
			{
				s = c曲リストノード.arScore[ nL12345 ].SongInformation.ArtistName;
			}
			Debug.WriteLine( s + ":" + c曲リストノード.strTitle );
		}
	}
	
	//-----------------
	#endregion
	#region [ .score.ini を読み込んで Cスコア.譜面情報に設定する ]
	//-----------------
	public void tReadScoreIniAndSetScoreInformation( string strScoreIniファイルパス, ref CScore score )
	{
		if( !File.Exists( strScoreIniファイルパス ) )
			return;

		try
		{
			var ini = new CScoreIni( strScoreIniファイルパス );
			ini.tCheckIntegrity();

			for( int n楽器番号 = 0; n楽器番号 < 3; n楽器番号++ )
			{
				int n = ( n楽器番号 * 2 ) + 1;	// n = 0～5

				#region socre.譜面情報.最大ランク[ n楽器番号 ] = ... 
				//-----------------
				if( ini.stSection[ n ].bMIDIUsed ||
				    ini.stSection[ n ].bKeyboardUsed ||
				    ini.stSection[ n ].bJoypadUsed ||
				    ini.stSection[ n ].bMouseUsed )
				{
					// (A) 全オートじゃないようなので、演奏結果情報を有効としてランクを算出する。
					if ( CDTXMania.ConfigIni.nSkillMode == 0 )
					{
						score.SongInformation.BestRank[ n楽器番号 ] =
							CScoreIni.tCalculateRankOld(
								ini.stSection[n].nTotalChipsCount,
								ini.stSection[n].nPerfectCount,
								ini.stSection[n].nGreatCount,
								ini.stSection[n].nGoodCount,
								ini.stSection[n].nPoorCount,
								ini.stSection[n].nMissCount
							);
					}
					else if( CDTXMania.ConfigIni.nSkillMode == 1 )
					{
						score.SongInformation.BestRank[ n楽器番号 ] =
							CScoreIni.tCalculateRank(
								ini.stSection[ n ].nTotalChipsCount,
								ini.stSection[ n ].nPerfectCount,
								ini.stSection[ n ].nGreatCount,
								ini.stSection[ n ].nGoodCount,
								ini.stSection[ n ].nPoorCount,
								ini.stSection[ n ].nMissCount,
								ini.stSection[ n ].nMaxCombo
							);
					}
				}
				else
				{
					// (B) 全オートらしいので、ランクは無効とする。
					score.SongInformation.BestRank[ n楽器番号 ] = (int) CScoreIni.ERANK.UNKNOWN;
				}
				//-----------------
				#endregion
				score.SongInformation.HighSkill[ n楽器番号 ] = ini.stSection[ n ].dbPerformanceSkill;
				score.SongInformation.HighSongSkill[ n楽器番号 ] = ini.stSection[ n ].dbGameSkill;
				score.SongInformation.FullCombo[ n楽器番号 ] = ini.stSection[ n ].bIsFullCombo | ini.stSection[ n楽器番号 * 2 ].bIsFullCombo;
				//New for Progress
				score.SongInformation.progress[n楽器番号] = ini.stSection[n].strProgress;					
				if(score.SongInformation.progress[n楽器番号] == "")
				{
					//TODO: Read from another file if progress string is empty
					//Set a hard-coded 64 char string for now
					score.SongInformation.progress[n楽器番号] = "0000000000000000000000000000000000000000000000000000000000000000";
				}
			}
			score.SongInformation.NbPerformances.Drums = ini.stFile.PlayCountDrums;
			score.SongInformation.NbPerformances.Guitar = ini.stFile.PlayCountGuitar;
			score.SongInformation.NbPerformances.Bass = ini.stFile.PlayCountBass;
			for( int i = 0; i < 5; i++ )
				score.SongInformation.PerformanceHistory[ i ] = ini.stFile.History[ i ];
		}
		catch
		{
			Trace.TraceError( "演奏記録ファイルの読み込みに失敗しました。[{0}]", strScoreIniファイルパス );
		}
	}
	//-----------------
	#endregion


	// Other

	#region [ private ]
	//-----------------
	private const string SONGSDB_VERSION = "SongsDB3(ver.K)rev2";
	private List<string> listStrBoxDefSkinSubfolderFullName;

	private int t比較0_共通( CSongListNode n1, CSongListNode n2 )
	{
		if( n1.eNodeType == CSongListNode.ENodeType.BACKBOX )
		{
			return -1;
		}
		if( n2.eNodeType == CSongListNode.ENodeType.BACKBOX )
		{
			return 1;
		}
		if( n1.eNodeType == CSongListNode.ENodeType.RANDOM )
		{
			return 1;
		}
		if( n2.eNodeType == CSongListNode.ENodeType.RANDOM )
		{
			return -1;
		}
		if( ( n1.eNodeType == CSongListNode.ENodeType.BOX ) && ( n2.eNodeType != CSongListNode.ENodeType.BOX ) )
		{
			return -1;
		}
		if( ( n1.eNodeType != CSongListNode.ENodeType.BOX ) && ( n2.eNodeType == CSongListNode.ENodeType.BOX ) )
		{
			return 1;
		}
		return 0;
	}

	/// <summary>
	/// 検索を中断_スローダウンする
	/// </summary>
	private void SlowOrSuspendSearchTask()
	{
		if ( bIsSuspending )		// #27060 中断要求があったら、解除要求が来るまで待機
		{
			autoReset.WaitOne();
		}
		if ( bIsSlowdown && ++searchCount > 10 )			// #27060 #PREMOVIE再生中は検索負荷を下げる
		{
			Thread.Sleep( 100 );
			searchCount = 0;
		}
	}

	//-----------------
	#endregion
}