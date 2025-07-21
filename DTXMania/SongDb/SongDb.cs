using System.Drawing;
using DTXMania.Core;

namespace DTXMania.SongDb;

public enum SongDbScanStatus
{
	Idle,
	Scanning,
	Processing
}

public class SongDb
{
	//public properties
	public SongDbScanStatus status { get; private set; } = SongDbScanStatus.Idle;

	public Dictionary<SongDbScanStatus, TimeSpan> statusDuration { get; private set; } = new()
	{
		{ SongDbScanStatus.Idle, TimeSpan.Zero },
		{ SongDbScanStatus.Scanning, TimeSpan.Zero },
		{ SongDbScanStatus.Processing, TimeSpan.Zero }
	};

	public SongNode songNodeRoot { get; private set; } = new(null!)
	{
		nodeType = SongNode.ENodeType.ROOT,
	};
	public int totalSongs { get; private set; } = 0;
	public int totalCharts { get; private set; } = 0;
	public string processSongDataPath { get; private set; } = string.Empty;
	public int processDoneCount { get; private set; } = 0;
	public int processTotalCount { get; private set; } = 0;
	
	private int tempCharts = 0;
	private int tempSongs = 0;

	public async Task ScanAsync(Action? onComplete = null)
	{
		SongNode tempRoot = new(null!) { nodeType = SongNode.ENodeType.ROOT };
		DateTime start = DateTime.Now;
		tempSongs = 0;
		tempCharts = 0;
		processSongDataPath = string.Empty;
		processDoneCount = 0;
		processTotalCount = 0;
		
		try
		{
			int maxThreadCount = Environment.ProcessorCount - 2;
			
			if (maxThreadCount < 2)
				maxThreadCount = 2;
			
			Console.WriteLine($"Starting song scan with {maxThreadCount} threads");
			status = SongDbScanStatus.Scanning;
			
			if (!string.IsNullOrEmpty(CDTXMania.ConfigIni.strSongDataSearchPath))
			{
				string[] paths = CDTXMania.ConfigIni.strSongDataSearchPath.Split([';']);
				if (paths.Length > 0)
				{
					await Parallel.ForEachAsync(paths, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
						async (path, cancellationToken) => await ScanSongsAsync(path, tempRoot.childNodes, tempRoot));
				}
			}
			
			statusDuration[SongDbScanStatus.Scanning] = DateTime.Now - start;
			
			//log time taken to scan
			Console.WriteLine($"Song scan completed in {statusDuration[SongDbScanStatus.Scanning]} s");
			Console.WriteLine($"Found {tempSongs} songs and {tempCharts} charts");
			
			//flatten songs so we can process them all sequentially. Include boxes since we want to generate back boxes.
			List<SongNode> flattened = await FlattenSongList(tempRoot.childNodes, true);
			
			Console.WriteLine($"Total song count after flattening: {flattened.Count}");
			
			processTotalCount = flattened.Count;
			
			status = SongDbScanStatus.Processing;
			
			start = DateTime.Now;
			
			await Parallel.ForEachAsync(flattened, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
				(song, cancellationToken) =>
				{
					ProcessListNode(song);
					return ValueTask.CompletedTask;
				});
			
			statusDuration[SongDbScanStatus.Processing] = DateTime.Now - start;
			Console.WriteLine($"Processed {tempSongs} songs and {tempCharts} charts");
			Console.WriteLine($"Processed full song list in {statusDuration[SongDbScanStatus.Processing]} s");
		}
		catch (Exception ex)
		{
			Console.WriteLine("An error occurred while scanning songs: " + ex.Message);
			status = SongDbScanStatus.Idle;
		}
		finally
		{
			songNodeRoot = tempRoot;
			
			totalSongs = tempSongs;
			totalCharts = tempCharts;
			
			status = SongDbScanStatus.Idle;
			onComplete?.Invoke();
		}
	}

	public async Task ScanSongsAsync(string searchPath, List<SongNode> targetList, SongNode parent)
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
			//if the directory starts with dtxfiles. it should be treated as a box
			if (infoDir.Name.ToLower().StartsWith("dtxfiles."))
			{
				SongNode node = new(parent)
				{
					nodeType = SongNode.ENodeType.BOX,
					title = infoDir.Name.Substring(9),
					path = infoDir.FullName + @"\",
					skinPath = parent.skinPath,
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
					]
				};
				
				targetList.Add(node);
				
				TryLoadBoxDef(node, infoDir);
				await ScanSongsAsync(infoDir.FullName + @"\", node.childNodes, node);
			}
			//if the folder contains a box.def file, handle it differently
			else if (File.Exists(infoDir.FullName + @"\box.def"))
			{
				SongNode node = new(parent)
				{
					nodeType = SongNode.ENodeType.BOX,
					path = infoDir.FullName + @"\",
					chartCount = 1,
					charts =
					[
						new CScore()
					]
				};
			
				node.charts[0].FileInformation.AbsoluteFolderPath = infoDir.FullName + @"\";
		
				targetList.Add(node);
		
				TryLoadBoxDef(node, infoDir);
				await ScanSongsAsync(infoDir.FullName + @"\", node.childNodes, node);
			}
			else
			//folder should not be treated as a box of any kind, just recursively scan its contents
			{
				await ScanSongsAsync(infoDir.FullName + @"\", targetList, parent);
			}
		}
	}

	private void TryLoadBoxDef(SongNode node, DirectoryInfo infoDir)
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

	private async Task ParseSetDef(string filePath, string baseFolder, List<SongNode> targetList, SongNode parent)
	{
		CSetDef def = new(filePath);

		try
		{
			//each block indicates a "song" which can contain multiple "charts" (scores)
			foreach (CSetDef.CBlock block in def.blocks)
			{
				SongNode song = new(parent)
				{
					nodeType = SongNode.ENodeType.SONG,
					title = block.Title,
					path = baseFolder + @"\",
					color = block.FontColor
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
					tempCharts++;
				}

				if (song.chartCount > 0)
				{
					targetList.Add(song);
					tempSongs++;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Failed to parse set.def file: " + filePath);
			Console.WriteLine(ex.Message);
		}
	}

	private void AddSongChart(List<SongNode> listNodeList, SongNode parent, FileInfo fileinfo)
	{
		SongNode songNode = new(parent)
		{
			nodeType = SongNode.ENodeType.SONG,
			chartCount = 1,
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

		listNodeList.Add(songNode);
		
		tempCharts++;
		tempSongs++;
	}
	
	public async Task<List<SongNode>> FlattenSongList(List<SongNode> toFlatten, bool includeBox = false)
	{
		List<SongNode> fullList = [];
		
		foreach (SongNode node in toFlatten)
		{
			if (node.nodeType == SongNode.ENodeType.BOX)
			{
				await AddChildrenToList(node, fullList, includeBox);
				
				if (includeBox)
				{
					fullList.Add(node);
				}
			}
			else if (node.nodeType == SongNode.ENodeType.SONG)
			{
				fullList.Add(node);
			}
		}

		return fullList;
	}
	
	private async Task AddChildrenToList(SongNode node, List<SongNode> fullList, bool includeBox = false)
	{
		if (node.childNodes is { Count: > 0 })
		{
			foreach (SongNode child in node.childNodes)
			{
				if (child.nodeType == SongNode.ENodeType.BOX)
				{
					await AddChildrenToList(child, fullList);
					
					if (includeBox)
					{
						fullList.Add(child);
					}
				}
				else
				{
					fullList.Add(child);
				}
			}
		}
	}
	
	private void ProcessListNode(SongNode node)
	{
		if (node.nodeType == SongNode.ENodeType.BOX)
		{
			//add a return node
			SongNode returnNode = new(node)
			{
				title = "<< BACK",
				nodeType = SongNode.ENodeType.BACKBOX,
				charts =
				{
					[0] = new CScore
					{
						FileInformation = new CScore.STFileInformation
						{
							AbsoluteFolderPath = ""
						},
						SongInformation = new CScore.STMusicInformation
						{
							Title = "<< BACK",
							Preimage = CSkin.Path(@"Graphics\5_preimage backbox.png"),
							Comment = CDTXMania.isJapanese ?
								"BOX を出ます。" :
								"Exit from the BOX."
						}
					}
				}
			};

			node.childNodes.Insert(0, returnNode);
			return;
		}
		
		for (int i = 0; i < 5; i++)
		{
			if (node.charts[i] == null || node.charts[i].bHadACacheInSongDB) continue;

			CScore score = node.charts[i];
			string path = score.FileInformation.AbsoluteFilePath;

			if (File.Exists(path))
			{
				try
				{
					processSongDataPath = path;
					CDTX cdtx = new(score.FileInformation.AbsoluteFilePath, false);
					
					if (string.IsNullOrWhiteSpace(node.title))
					{
						node.title = cdtx.TITLE;
					}

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
					score.SongInformation.bIsClassicChart.Drums = cdtx.bHasChips is { LeftCymbal: false, LP: false, LBD: false, FT: false, Ride: false };
					score.SongInformation.bIsClassicChart.Guitar = !cdtx.bHasChips.YPGuitar;
					score.SongInformation.bIsClassicChart.Bass = !cdtx.bHasChips.YPBass;
					score.SongInformation.bScoreExists.Drums = cdtx.bHasChips.Drums;
					score.SongInformation.bScoreExists.Guitar = cdtx.bHasChips.Guitar;
					score.SongInformation.bScoreExists.Bass = cdtx.bHasChips.Bass;
					score.SongInformation.SongType = cdtx.eFileType;
					score.SongInformation.Bpm = cdtx.BPM;
					score.SongInformation.Duration = (cdtx.listChip == null)
						? 0
						: cdtx.listChip[cdtx.listChip.Count - 1].nPlaybackTimeMs;

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

					cdtx.OnDeactivate();
				}
				catch (Exception exception)
				{
					Console.WriteLine("An error occurred while reading the song data file: " + path);
					Console.WriteLine("" + exception.Message);
					node.chartCount--;
					tempCharts--;
					continue;
				}
			}
			
			if (string.IsNullOrWhiteSpace(node.title))
			{
				node.title = node.path;
			}

			LoadScoreFile(score.FileInformation.AbsoluteFilePath + ".score.ini", ref score);
		}

		processDoneCount++;
	}

	private void LoadScoreFile(string path, ref CScore score)
	{
		if (!File.Exists(path))
			return;

		try
		{
			var ini = new CScoreIni(path);
			ini.tCheckIntegrity();

			for (int nInstrumentNumber = 0; nInstrumentNumber < 3; nInstrumentNumber++)
			{
				int n = (nInstrumentNumber * 2) + 1; // n = 0～5

				#region socre.譜面情報.最大ランク[ n楽器番号 ] = ...

				//-----------------
				if (ini.stSection[n].bMIDIUsed ||
				    ini.stSection[n].bKeyboardUsed ||
				    ini.stSection[n].bJoypadUsed ||
				    ini.stSection[n].bMouseUsed)
				{
					// (A) 全オートじゃないようなので、演奏結果情報を有効としてランクを算出する。
					if (CDTXMania.ConfigIni.nSkillMode == 0)
					{
						score.SongInformation.BestRank[nInstrumentNumber] =
							CScoreIni.tCalculateRankOld(
								ini.stSection[n].nTotalChipsCount,
								ini.stSection[n].nPerfectCount,
								ini.stSection[n].nGreatCount,
								ini.stSection[n].nGoodCount,
								ini.stSection[n].nPoorCount,
								ini.stSection[n].nMissCount
							);
					}
					else if (CDTXMania.ConfigIni.nSkillMode == 1)
					{
						score.SongInformation.BestRank[nInstrumentNumber] =
							CScoreIni.tCalculateRank(
								ini.stSection[n].nTotalChipsCount,
								ini.stSection[n].nPerfectCount,
								ini.stSection[n].nGreatCount,
								ini.stSection[n].nGoodCount,
								ini.stSection[n].nPoorCount,
								ini.stSection[n].nMissCount,
								ini.stSection[n].nMaxCombo
							);
					}
				}
				else
				{
					// (B) 全オートらしいので、ランクは無効とする。
					score.SongInformation.BestRank[nInstrumentNumber] = (int)CScoreIni.ERANK.UNKNOWN;
				}

				//-----------------

				#endregion

				score.SongInformation.HighSkill[nInstrumentNumber] = ini.stSection[n].dbPerformanceSkill;
				score.SongInformation.HighSongSkill[nInstrumentNumber] = ini.stSection[n].dbGameSkill;
				score.SongInformation.FullCombo[nInstrumentNumber] = ini.stSection[n].bIsFullCombo | ini.stSection[nInstrumentNumber * 2].bIsFullCombo;
				
				//New for Progress
				score.SongInformation.progress[nInstrumentNumber] = ini.stSection[n].strProgress;
				if (score.SongInformation.progress[nInstrumentNumber] == "")
				{
					//TODO: Read from another file if progress string is empty
					//Set a hard-coded 64 char string for now
					score.SongInformation.progress[nInstrumentNumber] =
						"0000000000000000000000000000000000000000000000000000000000000000";
				}
			}

			score.SongInformation.NbPerformances.Drums = ini.stFile.PlayCountDrums;
			score.SongInformation.NbPerformances.Guitar = ini.stFile.PlayCountGuitar;
			score.SongInformation.NbPerformances.Bass = ini.stFile.PlayCountBass;
			
			for (int i = 0; i < 5; i++)
				score.SongInformation.PerformanceHistory[i] = ini.stFile.History[i];
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to read score.ini file: " + path);
			Console.WriteLine(e.Message);
		}
	}
}