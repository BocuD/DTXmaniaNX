using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using Kawazu;

namespace DTXMania.SongDb;

public enum SongDbScanStatus
{
	Idle,
	Scanning,
	Unpacking,
	Processing
}

public class SongDb : IDisposable
{
	//public properties
	public SongDbScanStatus status { get; private set; } = SongDbScanStatus.Idle;
	
	private SongCacheSqlite songCache;

	public DateTime lastFinishTime;
	public Dictionary<SongDbScanStatus, TimeSpan> statusDuration { get; private set; } = new()
	{
		{ SongDbScanStatus.Idle, TimeSpan.Zero },
		{ SongDbScanStatus.Scanning, TimeSpan.Zero },
		{ SongDbScanStatus.Unpacking, TimeSpan.Zero },
		{ SongDbScanStatus.Processing, TimeSpan.Zero }
	};

	public SongNode songNodeRoot { get; private set; } = new(null, SongNode.ENodeType.ROOT);

	public List<SongNode> flattenedSongList { get; private set; }  = [];
	
	public List<(SongNode node, CChartData chart, double skill, int inst)> skillSongs = [];
	
	public bool hasEverScanned { get; private set; }
	public int totalSongs { get; private set; } = 0;
	public int totalCharts { get; private set; } = 0;
	public string processSongDataPath { get; private set; } = string.Empty;
	public string processUnpackZipFile { get; private set; } = string.Empty;
	public int processDoneCount { get; private set; } = 0;
	public int processTotalCount { get; private set; } = 0;
	
	// Cache statistics
	public int cacheHitCount { get; private set; } = 0;
	public int cacheMissCount { get; private set; } = 0;
	
	private int tempCharts = 0;
	private int tempSongs = 0;
	private DateTime start;
	private Task scanTask;

	public SongDb()
	{
		try
		{
			songCache = new SongCacheSqlite();
			songCache.Initialize();
			Trace.TraceInformation("Song database cache initialized");
		}
		catch (Exception ex)
		{
			Trace.TraceError($"Failed to initialize song cache, continuing without cache: {ex.Message}\n{ex.StackTrace}");
			songCache = null;
		}
	}

	public void StartScan(Action? onComplete = null)
	{
		Trace.TraceInformation("Scheduling song scan task...");
		scanTask = ScanAsync(onComplete);
	}

	private async Task ScanAsync(Action? onComplete = null)
	{
		processSongDataPath = string.Empty;
		processDoneCount = 0;
		processTotalCount = 0;
		
		int maxThreadCount = Environment.ProcessorCount - 2;
		if (maxThreadCount < 2)
			maxThreadCount = 2;
		
		try
		{
			SongNode tempRoot = await RunFullSongScan();

			if (foundZipFiles.Count > 0)
			{
				switch (CDTXMania.ConfigIni.eUnpackSongs)
				{
					case CConfigIni.UnpackSongs.Always:
						tempRoot = await UnpackZipFiles(maxThreadCount, 5, tempRoot);
						break;
					
					case CConfigIni.UnpackSongs.Ask:
						string title = CDTXMania.isJapanese ? "曲データの解凍" : "Unpack Songs";
						string description = CDTXMania.isJapanese ? $"演奏データに{foundZipFiles.Count} zip ファイルが見つかりました。\nすべての zip ファイルを解凍しますか？\nこの動作は設定メニューで変更できます。" : $"{foundZipFiles.Count} zip files were found in your song directory.\nDo you want to unpack all zip files?\nYou can change this behavior in the config menu.";
						string[] options = CDTXMania.isJapanese ? ["はい", "いいえ"] : ["Yes", "No"];
						
						int choice = await Modal.ShowAsync(
							CDTXMania.persistentUIGroup,
							title,
							description,
							options);

						if (choice == 0)
							tempRoot = await UnpackZipFiles(maxThreadCount, 5, tempRoot);
						else
							Trace.TraceInformation("User chose not to unpack zip files.");
						break;
				}
			}

			//flatten songs so we can process them all sequentially. Include boxes since we want to generate back boxes.
			List<SongNode> flattened = await FlattenSongList(tempRoot.childNodes, true);
			
			Trace.TraceInformation($"Total song count after flattening: {flattened.Count}");
			
			processTotalCount = flattened.Count;
			status = SongDbScanStatus.Processing;
			
			start = DateTime.Now;
			
			await Parallel.ForEachAsync(flattened, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
				async (song, cancellationToken) =>
				{
					try
					{
						await ProcessListNode(song);
					}
					catch (Exception e)
					{
						Trace.TraceError($"An error occurred while processing song {song.title} ({song.path}): {e.Message}");
					}
				});
			
			statusDuration[SongDbScanStatus.Processing] = DateTime.Now - start;
			Trace.TraceInformation($"Processed {tempSongs} songs and {tempCharts} charts");
			Trace.TraceInformation($"Processed full song list in {statusDuration[SongDbScanStatus.Processing]} s");
			
			// Log cache statistics
			if (songCache != null)
			{
				var cachedCharts = songCache.GetCacheSize();
				Trace.TraceInformation($"Cache Statistics - Hits: {cacheHitCount}, Misses: {cacheMissCount}, Total Cached Charts: {cachedCharts}");
			}
			
			if (tempRoot.childNodes.Count == 0)
			{
				Trace.TraceInformation("No songs found during scan.");
				var noSongs = new SongNode(tempRoot, SongNode.ENodeType.SONG)
				{
					title = "No Songs Found"
				};
			}

			void SortBox(SongNode root)
			{
				SortList(ref root.childNodes);

				foreach (SongNode node in root.childNodes.Where(node => node.nodeType == SongNode.ENodeType.BOX))
				{
					SortBox(node);
				}
			}

			void SortList(ref List<SongNode> nodes)
			{
				//apply base sort mode to flattened song list
				nodes = nodes.OrderBy(song =>
				{
					return CDTXMania.ConfigIni.baseSortMode switch
					{
						CConfigIni.SongSortMode.Title => song.title,
						CConfigIni.SongSortMode.Path => song.path,
						_ => song.title
					};
				}).ToList();
			}
			
			SortBox(tempRoot);
			
			var tempFlattenedSongList = await FlattenSongList(tempRoot.childNodes);
			var tempSkillSongs = CalculateSkill(tempFlattenedSongList, out double totalSkill);
			
			SortList(ref tempFlattenedSongList);
			
			songNodeRoot = tempRoot;
			flattenedSongList = tempFlattenedSongList;
			skillSongs = tempSkillSongs;
			
			totalSongs = tempSongs;
			totalCharts = tempCharts;

			hasEverScanned = true;
			lastFinishTime = DateTime.Now;
			
			onComplete?.Invoke();
		}
		catch (Exception ex)
		{
			Trace.TraceError($"An error occurred while scanning songs: {ex.Message}\n{ex.StackTrace}");
			status = SongDbScanStatus.Idle;
		}
		finally
		{
			status = SongDbScanStatus.Idle;
		}
	}

	public static double totalSkill = 0;

	public void ClearCache()
	{
		if (songCache != null)
		{
			songCache.ClearAllCache();
			cacheHitCount = 0;
			cacheMissCount = 0;
			Trace.TraceInformation("Song database cache cleared by user request");
		}
	}

	public void RecalculateSkill()
	{
		skillSongs = CalculateSkill(flattenedSongList, out totalSkill);
	}
	
	private static List<(SongNode node, CChartData chart, double skill, int inst)> CalculateSkill(List<SongNode> songList, out double totalSkill)
	{
		DateTime start = DateTime.Now;
		
		//calculate skill
		Trace.TraceInformation("Calculating skill songs and total skill...");

		List<(SongNode node, CChartData chart, double skill, int inst)> tempSkillSongs = [];

		foreach (SongNode node in songList)
		{
			(CChartData chart, double skill, double max, int inst) = node.GetTopSkillPoints();
			if (skill > 0)
			{
				tempSkillSongs.Add((node, chart, skill, inst));
			}
		}
        
		//sort by skill descending
		tempSkillSongs = tempSkillSongs.OrderByDescending(s => s.skill).ToList();
        
		//cut to top 50
		tempSkillSongs = tempSkillSongs.Take(50).ToList();
			
		//update skill state, calculate skill
		totalSkill = tempSkillSongs.Sum(song => song.skill);

		foreach (var song in tempSkillSongs)
		{
			song.chart.countSkill = true;
		}
		
		TimeSpan duration = DateTime.Now - start;
		Trace.TraceInformation($"Calculated skill for {tempSkillSongs.Count} songs in {duration.Milliseconds} ms");
		Trace.TraceInformation($"Total skill points across all ({tempSkillSongs.Count}) skill songs: {totalSkill}");
		return tempSkillSongs;
	}

	private async Task<SongNode> RunFullSongScan()
	{
		start = DateTime.Now;
		tempSongs = 0;
		tempCharts = 0;
		foundZipFiles.Clear();
		
		int maxThreadCount = Environment.ProcessorCount - 2;
		if (maxThreadCount < 2)
			maxThreadCount = 2;

		Trace.TraceInformation($"Starting song data scan with {maxThreadCount} threads");
		status = SongDbScanStatus.Scanning;

		SongNode tempRoot = new(null, SongNode.ENodeType.ROOT);

		if (!string.IsNullOrEmpty(CDTXMania.ConfigIni.strSongDataSearchPath))
		{
			string[] paths = CDTXMania.ConfigIni.strSongDataSearchPath.Split([';']);
			if (paths.Length > 0)
			{
				await Parallel.ForEachAsync(paths, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
					async (path, cancellationToken) => await ScanDirectoryAsyncRecursive(path, tempRoot));
			}
		}

		statusDuration[SongDbScanStatus.Scanning] = DateTime.Now - start;

		//log time taken to scan
		Trace.TraceInformation($"Song scan completed in {statusDuration[SongDbScanStatus.Scanning]} s");
		Trace.TraceInformation($"Found {tempSongs} songs and {tempCharts} charts");

		return tempRoot;
	}

	private async Task ScanDirectoryAsyncRecursive(string searchPath, SongNode parent)
	{
		if (!searchPath.EndsWith(@"\"))
			searchPath += @"\";
		
		int maxThreadCount = Environment.ProcessorCount - 2;
		if (maxThreadCount < 2)
			maxThreadCount = 2;

		DirectoryInfo info = new(searchPath);

		//option A: If set.def exists in the folder, create nodes from set.def
		string path = searchPath + "set.def";
		if (File.Exists(path))
		{
			ParseSetDef(path, searchPath, parent);
		}

		//option B: If set.def does not exist in the folder, there are probably chart files in the folder
		else
		{
			//loop over all files, try to load them as charts
			foreach (FileInfo fileinfo in info.GetFiles())
			{
				try
				{
					string strExt = fileinfo.Extension.ToLower();
					switch (strExt)
					{
						case ".dtx":
						case ".gda":
						case ".g2d":
						case ".bms":
						case ".bme":
							AddSongChart(parent, fileinfo);
							break;
						
						case ".zip":
							foundZipFiles.Add(fileinfo);
							break;

						case ".mid":
						case ".smf":
							// DoNothing
							//????
							break;
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError($"Failed to process file {fileinfo.FullName}: {ex.Message}");
				}
			}
		}

		DirectoryInfo[] directories = info.GetDirectories();
		await Parallel.ForEachAsync(directories, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
			async (directory, cancellationToken) => await ScanDirectory(directory));
		
		return;

		//scan subdirectories
		async Task ScanDirectory(DirectoryInfo infoDir)
		{
			try
			{
				//if the directory starts with dtxfiles. it should be treated as a box
				if (infoDir.Name.ToLower().StartsWith("dtxfiles."))
				{
					SongNode node = new(parent, SongNode.ENodeType.BOX)
					{
						title = infoDir.Name.Substring(9),
						path = infoDir.FullName + @"\",
						skinPath = parent.skinPath,
						charts =
						[
							new CChartData
							{
								FileInformation = new CChartData.STFileInformation
								{
									AbsoluteFolderPath = infoDir.FullName + @"\"
								},
								SongInformation = new CChartData.STMusicInformation
								{
									Title = infoDir.Name.Substring(9),
									Comment = CDTXMania.isJapanese ? "BOX に移動します。" : "Enter into the BOX."
								}
							}
						]
					};

					TryLoadBoxDef(node, infoDir);
					await ScanDirectoryAsyncRecursive(infoDir.FullName + @"\", node);
				}
				//if the folder contains a box.def file, handle it differently
				else if (File.Exists(infoDir.FullName + @"\box.def"))
				{
					SongNode node = new(parent, SongNode.ENodeType.BOX)
					{
						path = infoDir.FullName + @"\",
						chartCount = 1,
						charts =
						[
							new CChartData()
						]
					};

					node.charts[0].FileInformation.AbsoluteFolderPath = infoDir.FullName + @"\";

					TryLoadBoxDef(node, infoDir);
					await ScanDirectoryAsyncRecursive(infoDir.FullName + @"\", node);
				}
				else //folder should not be treated as a box of any kind, just recursively scan its contents
				{
					await ScanDirectoryAsyncRecursive(infoDir.FullName + @"\", parent);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError($"Failed to process directory {infoDir.FullName}: {ex.Message}");
				Trace.TraceError($"Stack trace: {ex.StackTrace}");
			}
		}
	}
	
	private readonly List<FileInfo> foundZipFiles = [];
	
	private async Task<SongNode> UnpackZipFiles(int maxThreadCount, int maxLoopCount, SongNode tempRoot)
	{
		processDoneCount = 0;
		
		while (foundZipFiles.Count > 0 && maxLoopCount-- > 0)
		{
			status = SongDbScanStatus.Unpacking;
			start = DateTime.Now;
			processTotalCount = foundZipFiles.Count;
			
			Trace.TraceInformation($"Unpacking {foundZipFiles.Count} zip files...");
				
			await Parallel.ForEachAsync(foundZipFiles, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
				async (info, cancellationToken) => await UnpackZipFile(info));
				
			statusDuration[SongDbScanStatus.Unpacking] = DateTime.Now - start;
			Trace.TraceInformation($"Unpacked {foundZipFiles.Count} zip files in {statusDuration[SongDbScanStatus.Unpacking]} s");
				
			Trace.TraceInformation($"Rescanning because ZIP files were unpacked");
			tempRoot = await RunFullSongScan();
		}
		
		Trace.TraceInformation($"Finished unpacking {processDoneCount} zip files");
		
		//reset count after unzipping
		processDoneCount = 0;
		
		return tempRoot;
	}
	
	private async Task UnpackZipFile(FileInfo fileinfo)
	{
		Trace.TraceInformation("Found zip file in song database, unpacking: " + fileinfo.FullName);

		processUnpackZipFile = fileinfo.FullName;
		//List<string> newlyCreatedDirectories = [];
		try
		{
			{
				using ZipArchive zip = new(fileinfo.OpenRead(), ZipArchiveMode.Read);

				//HashSet<string> rootDirectories = []; //to track unique root directories

				foreach (ZipArchiveEntry entry in zip.Entries)
				{
					if (string.IsNullOrEmpty(entry.Name)) continue;

					string entryPath = Path.Combine(fileinfo.DirectoryName!, entry.FullName);
					
					//skip directories: if directories need to be created for files we will create them below
					if (entry.FullName.EndsWith(@"\")) continue;
					
					//ensure directory exists
					string directory = Path.GetDirectoryName(entryPath)!;
					Directory.CreateDirectory(directory);
					entry.ExtractToFile(entryPath, true);
					
					// string rootDirectory = Path.Combine(fileinfo.DirectoryName!, entry.FullName.Split(Path.DirectorySeparatorChar)[0]);
					// if (rootDirectories.Add(rootDirectory)) //add to HashSet and check if it's new
					// {
					// 	newlyCreatedDirectories.Add(rootDirectory);
					// }
				}
			}

			//success: delete the zip file
			Trace.TraceInformation("Successfully unpacked zip file: " + fileinfo.FullName);
			fileinfo.Delete();
			processDoneCount++;
		}
		catch (Exception ex)
		{
			Trace.TraceError($"Failed to unpack zip file {fileinfo.FullName}: {ex.Message}");
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

	private void ParseSetDef(string filePath, string baseFolder, SongNode parent)
	{
		CSetDef def = new(filePath);

		try
		{
			//each block indicates a "song" which can contain multiple "charts" (scores)
			foreach (CSetDef.CBlock block in def.blocks)
			{
				SongNode song = new(parent, SongNode.ENodeType.SONG)
				{
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
							
					song.charts[j] = new CChartData();
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

				if (song.chartCount <= 0)
				{
					parent.childNodes.Remove(song);
				}
				else
				{
					tempSongs++;
				}
			}
		}
		catch (Exception ex)
		{
			Trace.TraceError("Failed to parse set.def file: " + filePath);
			Trace.TraceError(ex.Message);
		}
	}

	private void AddSongChart(SongNode parent, FileInfo fileinfo)
	{
		SongNode songNode = new(parent, SongNode.ENodeType.SONG)
		{
			chartCount = 1,
			path = fileinfo.FullName + @"\",
			charts =
			{
				[0] = new CChartData
				{
					FileInformation = new CChartData.STFileInformation
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
		
		tempCharts++;
		tempSongs++;
	}
	
	public async Task<List<SongNode>> FlattenSongList(List<SongNode> toFlatten, bool includeBox = false)
	{
		List<SongNode> fullList = [];
		
		foreach (SongNode node in toFlatten)
		{
			switch (node.nodeType)
			{
				case SongNode.ENodeType.BOX:
				{
					await AddChildrenToList(node, fullList, includeBox);
				
					if (includeBox)
					{
						fullList.Add(node);
					}
					break;
				}
				case SongNode.ENodeType.SONG:
					fullList.Add(node);
					break;
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
				if (child.nodeType is SongNode.ENodeType.BOX)
				{
					await AddChildrenToList(child, fullList);
					
					if (includeBox)
					{
						fullList.Add(child);
					}
				}
				else if (child.nodeType != SongNode.ENodeType.BACKBOX)
				{
					fullList.Add(child);
				}
			}
		}
	}
	
	private async Task ProcessListNode(SongNode node)
	{
		if (node.nodeType == SongNode.ENodeType.SONG)
		{
			for (int i = 0; i < 5; i++)
			{
				if (node.charts[i] == null || node.charts[i].bHadACacheInSongDB) continue;

				CChartData chartData = node.charts[i];
				string path = chartData.FileInformation.AbsoluteFilePath;
				string scoreIniPath = path + ".score.ini";

				if (File.Exists(path))
				{
					// Try to get from cache first
					if (songCache != null && songCache.TryGetCachedChart(path, scoreIniPath, out CChartData cachedData))
					{
						node.charts[i] = cachedData;
						
						if (string.IsNullOrWhiteSpace(node.title))
						{
							node.title = cachedData.SongInformation.Title;
						}
						
						node.charts[i].LoadScoreFile();

						cacheHitCount++;
						processDoneCount++;
						continue;
					}

					cacheMissCount++;

					try
					{
						processSongDataPath = path;
						CDTX cdtx = new(chartData.FileInformation.AbsoluteFilePath, false);

						if (string.IsNullOrWhiteSpace(node.title))
						{
							node.title = cdtx.TITLE;
						}

						chartData.SongInformation.Title = cdtx.TITLE;
						chartData.SongInformation.ArtistName = cdtx.ARTIST;
						chartData.SongInformation.Comment = cdtx.COMMENT;

						if (Utilities.HasJapanese(chartData.SongInformation.Title))
						{
							chartData.SongInformation.TitleHasJapanese = true;
							
							// Use cache for Japanese conversions
							var converted = TextConversionCache.GetOrCacheTextConversion(chartData.SongInformation.Title);
							
							chartData.SongInformation.TitleKana = converted.kana;
							chartData.SongInformation.TitleRoman = converted.romaji;
						}
						else
						{
							chartData.SongInformation.TitleKana = chartData.SongInformation.Title;
							chartData.SongInformation.TitleRoman = chartData.SongInformation.Title.ToLowerInvariant();
						}

						if (Utilities.HasJapanese(chartData.SongInformation.ArtistName))
						{
							chartData.SongInformation.ArtistNameHasJapanese = true;

							var converted = TextConversionCache.GetOrCacheTextConversion(chartData.SongInformation.ArtistName);
							
							chartData.SongInformation.ArtistNameKana = converted.kana;
							chartData.SongInformation.ArtistNameRoman = converted.romaji;
						}
						else
						{
							chartData.SongInformation.ArtistNameKana = chartData.SongInformation.ArtistName;
							chartData.SongInformation.ArtistNameRoman = chartData.SongInformation.ArtistName.ToLowerInvariant();
						}
						
						if (Utilities.HasJapanese(chartData.SongInformation.Comment))
						{
							chartData.SongInformation.CommentHasJapanese = true;
							
							var converted = TextConversionCache.GetOrCacheTextConversion(chartData.SongInformation.Comment);
							
							chartData.SongInformation.CommentKana = converted.kana;
							chartData.SongInformation.CommentRoman = converted.romaji;
						}
						else
						{
							chartData.SongInformation.CommentKana = chartData.SongInformation.Comment;
							chartData.SongInformation.CommentRoman = chartData.SongInformation.Comment.ToLowerInvariant();
						}

						chartData.SongInformation.Genre = cdtx.GENRE;
						chartData.SongInformation.Preimage = cdtx.PREIMAGE;
						chartData.SongInformation.Premovie = cdtx.PREMOVIE;
						chartData.SongInformation.Presound = cdtx.PREVIEW;
						chartData.SongInformation.Backgound =
							cdtx.BACKGROUND is { Length: > 0 } ? cdtx.BACKGROUND : cdtx.BACKGROUND_GR;
						chartData.SongInformation.Level.Drums = cdtx.LEVEL.Drums;
						chartData.SongInformation.Level.Guitar = cdtx.LEVEL.Guitar;
						chartData.SongInformation.Level.Bass = cdtx.LEVEL.Bass;
						chartData.SongInformation.LevelDec.Drums = cdtx.LEVELDEC.Drums;
						chartData.SongInformation.LevelDec.Guitar = cdtx.LEVELDEC.Guitar;
						chartData.SongInformation.LevelDec.Bass = cdtx.LEVELDEC.Bass;
						chartData.SongInformation.bHiddenLevel = cdtx.HIDDENLEVEL;
						chartData.SongInformation.bIsClassicChart.Drums = cdtx.bHasChips is
							{ LeftCymbal: false, LP: false, LBD: false, FT: false, Ride: false };
						chartData.SongInformation.bIsClassicChart.Guitar = !cdtx.bHasChips.YPGuitar;
						chartData.SongInformation.bIsClassicChart.Bass = !cdtx.bHasChips.YPBass;
						chartData.SongInformation.bScoreExists.Drums = cdtx.bHasChips.Drums;
						chartData.SongInformation.bScoreExists.Guitar = cdtx.bHasChips.Guitar;
						chartData.SongInformation.bScoreExists.Bass = cdtx.bHasChips.Bass;
						chartData.SongInformation.SongType = cdtx.eFileType;
						chartData.SongInformation.Bpm = cdtx.BPM;
						chartData.SongInformation.DurationMs = (cdtx.listChip == null)
							? 0
							: cdtx.listChip[cdtx.listChip.Count - 1].nPlaybackTimeMs;

						chartData.SongInformation.chipCountByInstrument.Drums = cdtx.nVisibleChipsCount.Drums;
						{
							chartData.SongInformation.chipCountByLane[ELane.LC] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.LC);
							chartData.SongInformation.chipCountByLane[ELane.HH] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.HH);
							chartData.SongInformation.chipCountByLane[ELane.SD] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.SD);
							chartData.SongInformation.chipCountByLane[ELane.LP] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.LP);
							chartData.SongInformation.chipCountByLane[ELane.HT] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.HT);
							chartData.SongInformation.chipCountByLane[ELane.BD] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BD);
							chartData.SongInformation.chipCountByLane[ELane.LT] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.LT);
							chartData.SongInformation.chipCountByLane[ELane.FT] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.FT);
							chartData.SongInformation.chipCountByLane[ELane.CY] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.CY);
						}
						
						chartData.SongInformation.chipCountByInstrument.Guitar = cdtx.nVisibleChipsCount.Guitar;
						{
							chartData.SongInformation.chipCountByLane[ELane.GtR] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtR);
							chartData.SongInformation.chipCountByLane[ELane.GtG] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtG);
							chartData.SongInformation.chipCountByLane[ELane.GtB] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtB);
							chartData.SongInformation.chipCountByLane[ELane.GtY] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtY);
							chartData.SongInformation.chipCountByLane[ELane.GtP] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtP);
							chartData.SongInformation.chipCountByLane[ELane.GtPick] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.GtPick);
						}

						chartData.SongInformation.chipCountByInstrument.Bass = cdtx.nVisibleChipsCount.Bass;
						{
							chartData.SongInformation.chipCountByLane[ELane.BsR] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsR);
							chartData.SongInformation.chipCountByLane[ELane.BsG] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsG);
							chartData.SongInformation.chipCountByLane[ELane.BsB] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsB);
							chartData.SongInformation.chipCountByLane[ELane.BsY] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsY);
							chartData.SongInformation.chipCountByLane[ELane.BsP] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsP);
							chartData.SongInformation.chipCountByLane[ELane.BsPick] =
								cdtx.nVisibleChipsCount.chipCountInLane(ELane.BsPick);
						}

						cdtx.OnDeactivate();

						//save to cache after successful processing
						if (songCache != null)
						{
							songCache.SaveChartData(path, scoreIniPath, chartData);
						}
					}
					catch (Exception exception)
					{
						Trace.TraceError("An error occurred while reading the song data file: " + path);
						Trace.TraceError("" + exception.Message);
						node.chartCount--;
						tempCharts--;
						processDoneCount++;
						continue;
					}
				}

				if (string.IsNullOrWhiteSpace(node.title))
				{
					node.title = node.path;
				}

				chartData.LoadScoreFile();
			}
		}

		processDoneCount++;
	}

	public void Dispose()
	{
		songCache?.Dispose();
	}
}
