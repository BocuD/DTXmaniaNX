using System.Drawing;
using DTXMania.Core;

namespace DTXMania.SongDb;

public class SongNode
{
    public enum ENodeType
    {
        SONG,
        BOX
    }
    
    public ENodeType NodeType { get; set; } = ENodeType.SONG;
    public string skinPath = string.Empty;
    
    public string title = string.Empty;
    public string path = string.Empty;
    public Color color = Color.White;

    public SongNode? parent = null;
    public List<SongNode>? childNodes = null;

    public int chartCount;
    public CScore[] charts = new CScore[5];
    public string[] difficultyLabel = new string[5];
    
    public STHitRanges stDrumHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stDrumPedalHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stGuitarHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stBassHitRanges = new(nDefaultSizeMs: -1);
}

public class SongDb
{
	public bool scanning = false;
	public List<SongNode> songNodeRoot = [];

	public async Task ScanAsync()
	{
		try
		{
			//clear
			scanning = true;
			songNodeRoot.Clear();

			List<Task> tasks = [];

			if (!string.IsNullOrEmpty(CDTXMania.ConfigIni.str曲データ検索パス))
			{
				string[] strArray = CDTXMania.ConfigIni.str曲データ検索パス.Split([';']);
				if (strArray.Length > 0)
				{
					foreach (string str in strArray)
					{
						if (!string.IsNullOrEmpty(str))
						{
							tasks.Add(ScanSongsAsync(str, songNodeRoot));
						}
					}
				}
			}

			await Task.WhenAll(tasks);
		}
		catch (Exception ex)
		{
			Console.WriteLine("An error occurred while scanning songs: " + ex.Message);
			scanning = false;
		}
		finally
		{
			scanning = false;
		}
	}

	public async Task ScanSongsAsync(string searchPath, List<SongNode> targetList, SongNode? parent = null)
	{
		if (!searchPath.EndsWith(@"\"))
			searchPath += @"\";

		DirectoryInfo info = new(searchPath);

		//option A: If set.def exists in the folder, create nodes from set.def
		string path = searchPath + "set.def";
		if (File.Exists(path))
		{
			await ParseSetDef(path, searchPath, targetList, parent);
		}

		//option B: If set.def does not exist in the folder, there are probably chart files in the folder
		else
		{
			//loop over all files, try to load them as charts
			foreach (FileInfo fileinfo in info.GetFiles())
			{
				string strExt = fileinfo.Extension.ToLower();
				switch (strExt)
				{
					case ".dtx":
					case ".gda":
					case ".g2d":
					case ".bms":
					case ".bme":
						AddSongChart(targetList, parent, fileinfo);
						break;
					
					case ".mid":
					case ".smf":
						// DoNothing
						//????
						break;
				}
			}
		}

		//scan subdirectories
		foreach (DirectoryInfo infoDir in info.GetDirectories())
		{
			#region [ a. "dtxfiles." で始まるフォルダの場合 ]

			//-----------------------------
			if (infoDir.Name.ToLower().StartsWith("dtxfiles."))
			{
				SongNode node = new()
				{
					NodeType = SongNode.ENodeType.BOX,
					title = infoDir.Name.Substring(9),
					path = infoDir.FullName + @"\",
					parent = parent,
					skinPath = parent == null
						? string.Empty
						: parent.skinPath,
					charts = 
					[
						new CScore
						{
							FileInformation = new CScore.STFileInformation
							{
								AbsoluteFolderPath = infoDir.FullName + @"\"
							},
							SongInformation = new CScore.STMusicInformation
							{
								Title = infoDir.Name.Substring(9),
								Comment = CDTXMania.isJapanese ? "BOX に移動します。" : "Enter into the BOX."
							}
						}
					],
					childNodes = []
				};
				
				targetList.Add(node);
				
				await TryLoadBoxDef(node, infoDir);
				
				await ScanSongsAsync(infoDir.FullName + @"\", node.childNodes, node);
			}
			//-----------------------------

			#endregion

			#region [ b.box.def を含むフォルダの場合  ]

			//if the folder contains a box.def file, handle it differently
			else if (File.Exists(infoDir.FullName + @"\box.def"))
			{
				SongNode node = new()
				{
					NodeType = SongNode.ENodeType.BOX,
					path = infoDir.FullName + @"\",
					chartCount = 1,
					charts =
					[
						new CScore()
					]
				};
			
				node.charts[0].FileInformation.AbsoluteFolderPath = infoDir.FullName + @"\";
				node.parent = parent;
				node.childNodes = [];
		
				targetList.Add(node);
		
				await TryLoadBoxDef(node, infoDir);
				await ScanSongsAsync(infoDir.FullName + @"\", node.childNodes, node);
			}
			//-----------------------------

			#endregion

			#region [ c.通常フォルダの場合 ]

			//-----------------------------
			else
			{
				await ScanSongsAsync(infoDir.FullName + @"\", targetList, parent);
			}

			//-----------------------------

			#endregion
		}
	}

	private async Task TryLoadBoxDef(SongNode node, DirectoryInfo infoDir)
	{
		string boxDefPath = infoDir.FullName + @"\box.def";
		if (File.Exists(boxDefPath))
		{
			CBoxDef boxdef = new(boxDefPath);
			
			if (boxdef.Title is { Length: > 0 })
			{
				node.title = boxdef.Title;
				node.charts[0].SongInformation.Title = boxdef.Title;
			}

			if (boxdef.Color != Color.White)
			{
				node.color = boxdef.Color;
			}

			if (boxdef.Artist is { Length: > 0 })
			{
				node.charts[0].SongInformation.ArtistName = boxdef.Artist;
			}

			if (boxdef.Comment is { Length: > 0 })
			{
				node.charts[0].SongInformation.Comment = boxdef.Comment;
			}

			if (boxdef.Preimage is { Length: > 0 })
			{
				node.charts[0].SongInformation.Preimage = boxdef.Preimage;
			}

			if (boxdef.Premovie is { Length: > 0 })
			{
				node.charts[0].SongInformation.Premovie = boxdef.Premovie;
			}

			if (boxdef.Presound is { Length: > 0 })
			{
				node.charts[0].SongInformation.Presound = boxdef.Presound;
			}

			if (!string.IsNullOrWhiteSpace(boxdef.SkinPath))
			{
				// box.defに記載されているスキン情報をコピー。末尾に必ず\をつけておくこと。
				string skinPath = Path.Combine(infoDir.FullName, boxdef.SkinPath);
				
				if (skinPath[skinPath.Length - 1] != Path.DirectorySeparatorChar) // フォルダ名末尾に\を必ずつけて、CSkin側と表記を統一する
				{
					skinPath += Path.DirectorySeparatorChar;
				}

				if (CDTXMania.Skin.bIsValid(skinPath))
				{
					node.skinPath = skinPath;
				}
			}

			// copy hit ranges from the box.def
			// these can always be copied regardless of being set,
			// as song list nodes and boxdefs use the same method to indicate an unset range
			node.stDrumHitRanges = boxdef.stDrumHitRanges;
			node.stDrumPedalHitRanges = boxdef.stDrumPedalHitRanges;
			node.stGuitarHitRanges = boxdef.stGuitarHitRanges;
			node.stBassHitRanges = boxdef.stBassHitRanges;
		}
	}

	private async Task ParseSetDef(string filePath, string baseFolder, List<SongNode> targetList, SongNode? parent)
	{
		CSetDef def = new(filePath);

		try
		{
			//each block indicates a "song" which can contain multiple "charts" (scores)
			foreach (CSetDef.CBlock block in def.blocks)
			{
				SongNode song = new()
				{
					NodeType = SongNode.ENodeType.SONG,
					title = block.Title,
					path = baseFolder + @"\",
					color = block.FontColor,
					parent = parent
				};

				for (int j = 0; j < 5; j++)
				{
					if (string.IsNullOrEmpty(block.File[j])) continue;
					
					string chartPath = baseFolder + block.File[j];
					if (!File.Exists(chartPath)) continue;
						
					song.difficultyLabel[j] = block.Label[j];
							
					song.charts[j] = new CScore();
					song.charts[j].FileInformation.AbsoluteFilePath = chartPath;
					song.charts[j].FileInformation.AbsoluteFolderPath = Path.GetFullPath(Path.GetDirectoryName(chartPath)!) + @"\";
							
					FileInfo info = new(chartPath);
					song.charts[j].FileInformation.FileSize = info.Length;
					song.charts[j].FileInformation.LastModified = info.LastWriteTime;
							
					string scorePath = chartPath + ".score.ini";
					if (File.Exists(scorePath))
					{
						FileInfo info3 = new(scorePath);
						song.charts[j].ScoreIniInformation.FileSize = info3.Length;
						song.charts[j].ScoreIniInformation.LastModified = info3.LastWriteTime;
					}

					song.chartCount++;
					nChartCount++;
				}

				if (song.chartCount > 0)
				{
					targetList.Add(song);
					nSongCount++;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Failed to parse set.def file: " + filePath);
			Console.WriteLine(ex.Message);
		}
	}

	private void AddSongChart(List<SongNode> listNodeList, SongNode? nodeParent, FileInfo fileinfo)
	{
		SongNode songNode = new()
		{
			NodeType = SongNode.ENodeType.SONG,
			chartCount = 1,
			parent = nodeParent,
			path = fileinfo.FullName + @"\",
			charts =
			{
				[0] = new CScore
				{
					FileInformation = new CScore.STFileInformation
					{
						AbsoluteFilePath = fileinfo.FullName,
						AbsoluteFolderPath = Path.GetFullPath(Path.GetDirectoryName(fileinfo.FullName)!) + @"\",
						FileSize = fileinfo.Length,
						LastModified = fileinfo.LastWriteTime
					}
				}
			}
		};

		string strFileNameScoreIni = songNode.charts[0].FileInformation.AbsoluteFilePath + ".score.ini";
		if (File.Exists(strFileNameScoreIni))
		{
			FileInfo infoScoreIni = new(strFileNameScoreIni);
			songNode.charts[0].ScoreIniInformation.FileSize = infoScoreIni.Length;
			songNode.charts[0].ScoreIniInformation.LastModified = infoScoreIni.LastWriteTime;
		}

		nChartCount++;

		listNodeList.Add(songNode);
		nSongCount++;
	}
	
	public int nChartCount = 0; // Number of scores found
	public int nSongCount = 0; // Number of song nodes found
}