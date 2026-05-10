using System.Diagnostics;
using System.Runtime.InteropServices;
using DTXMania.Core;

namespace DTXMania;

[Serializable]
public class CChartData
{
    public STScoreIniInformation ScoreIniInformation;

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct STScoreIniInformation
    {
        public DateTime LastModified;
        public long FileSize;

        public STScoreIniInformation(DateTime lastModified, long fileSize)
        {
            LastModified = lastModified;
            FileSize = fileSize;
        }
    }

    public STFileInformation FileInformation;

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct STFileInformation
    {
        public string AbsoluteFilePath;
        public string AbsoluteFolderPath;
        public DateTime LastModified;
        public long FileSize;

        public STFileInformation(string AbsoluteFilePath, string AbsoluteFolderPath, DateTime LastModified,
            long FileSize)
        {
            this.AbsoluteFilePath = AbsoluteFilePath;
            this.AbsoluteFolderPath = AbsoluteFolderPath;
            this.LastModified = LastModified;
            this.FileSize = FileSize;
        }
    }

    public STMusicInformation SongInformation;

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct STMusicInformation
    {
        public string Title;
        public string ArtistName;
        public string Comment;

        public bool TitleHasJapanese;
        public bool ArtistNameHasJapanese;
        public bool CommentHasJapanese;

        public string TitleKana;
        public string ArtistNameKana;
        
        public string TitleRoman;
        public string ArtistNameRoman;
        
        public string CommentKana;
        public string CommentRoman;
        
        public string Genre;
        public string Preimage;
        public string Premovie;
        public string Presound;
        public string Backgound;
        public STDGBVALUE<int> Level;
        public STDGBVALUE<int> LevelDec;
        public STRANK BestRank;
        public STSKILL HighCompletionRate;
        public STSKILL HighSongSkill;
        public STDGBVALUE<bool> FullCombo;
        public STDGBVALUE<int> NbPerformances;
        public STHISTORY PerformanceHistory;
        public bool bHiddenLevel;
        public CDTX.EType SongType;
        public double Bpm;
        public int DurationMs;
        public STDGBVALUE<bool> bIsClassicChart;

        public STDGBVALUE<bool> bScoreExists;

        //
        //public STDGBVALUE<EUseLanes> 使用レーン数;
        public STDGBVALUE<int> chipCountByInstrument;
        public Dictionary<ELane, int> chipCountByLane;
        public STDGBVALUE<string> progress;

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct STHISTORY
        {
            public string row1;
            public string row2;
            public string row3;
            public string row4;
            public string row5;

            public string this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return row1;

                        case 1:
                            return row2;

                        case 2:
                            return row3;

                        case 3:
                            return row4;

                        case 4:
                            return row5;
                    }

                    throw new IndexOutOfRangeException();
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            row1 = value;
                            return;

                        case 1:
                            row2 = value;
                            return;

                        case 2:
                            row3 = value;
                            return;

                        case 3:
                            row4 = value;
                            return;

                        case 4:
                            row5 = value;
                            return;
                    }

                    throw new IndexOutOfRangeException();
                }
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct STRANK
        {
            public int Drums;
            public int Guitar;
            public int Bass;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return Drums;

                        case 1:
                            return Guitar;

                        case 2:
                            return Bass;
                    }

                    throw new IndexOutOfRangeException();
                }
                set
                {
                    if ((value < (int)CScoreIni.ERANK.SS) ||
                        ((value != (int)CScoreIni.ERANK.UNKNOWN) && (value > (int)CScoreIni.ERANK.E)))
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    switch (index)
                    {
                        case 0:
                            Drums = value;
                            return;

                        case 1:
                            Guitar = value;
                            return;

                        case 2:
                            Bass = value;
                            return;
                    }

                    throw new IndexOutOfRangeException();
                }
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct STSKILL
        {
            public double Drums;
            public double Guitar;
            public double Bass;

            public double this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return Drums;

                        case 1:
                            return Guitar;

                        case 2:
                            return Bass;
                    }

                    throw new IndexOutOfRangeException();
                }
                set
                {
                    if ((value < 0.0) || (value > 200.0))
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    switch (index)
                    {
                        case 0:
                            Drums = value;
                            return;

                        case 1:
                            Guitar = value;
                            return;

                        case 2:
                            Bass = value;
                            return;
                    }

                    throw new IndexOutOfRangeException();
                }
            }
        }

        public double GetLevel(int instrument)
        {
            double dbLevel = Level[instrument];
            int levelDec = LevelDec[instrument];
            if (dbLevel >= 100)
            {
                dbLevel /= 100.0;
            }
            else if (dbLevel < 100)
            {
                dbLevel = dbLevel / 10.0 + levelDec / 100.0;
            }

            return dbLevel;
        }

        public double GetMaxSkill(int instrument)
        {
            return GetLevel(instrument) * 20;
        }
    }

    public bool bHadACacheInSongDB;
    public bool countSkill = false;


    // Constructor

    public CChartData()
    {
        ScoreIniInformation = new STScoreIniInformation(DateTime.MinValue, 0L);
        bHadACacheInSongDB = false;
        FileInformation = new STFileInformation("", "", DateTime.MinValue, 0L);
        SongInformation = new STMusicInformation
        {
            Title = "",
            ArtistName = "",
            Comment = "",
            Genre = "",
            Preimage = "",
            Premovie = "",
            Presound = "",
            Backgound = "",
            Level = new STDGBVALUE<int>(),
            LevelDec = new STDGBVALUE<int>(),
            BestRank = new STMusicInformation.STRANK
            {
                Drums = (int)CScoreIni.ERANK.UNKNOWN,
                Guitar = (int)CScoreIni.ERANK.UNKNOWN,
                Bass = (int)CScoreIni.ERANK.UNKNOWN
            },
            FullCombo = new STDGBVALUE<bool>(),
            NbPerformances = new STDGBVALUE<int>(),
            PerformanceHistory = new STMusicInformation.STHISTORY
            {
                row1 = "",
                row2 = "",
                row3 = "",
                row4 = "",
                row5 = ""
            },
            bHiddenLevel = false,
            HighCompletionRate = new STMusicInformation.STSKILL(),
            HighSongSkill = new STMusicInformation.STSKILL(),
            SongType = CDTX.EType.DTX,
            Bpm = 120.0,
            DurationMs = 0,
            bIsClassicChart = new STDGBVALUE<bool>()
            {
                Drums = false,
                Guitar = false,
                Bass = false
            },
            bScoreExists = new STDGBVALUE<bool>()
            {
                Drums = false,
                Guitar = false,
                Bass = false
            },
            chipCountByInstrument = default,
            chipCountByLane = new Dictionary<ELane, int>(),
            progress = new STDGBVALUE<string>()
            {
                Drums = "",
                Guitar = "",
                Bass = ""
            }
        };

        for (ELane eLane = ELane.LC; eLane < ELane.BGM; eLane++)
        {
            SongInformation.chipCountByLane[eLane] = 0;
        }
    }

    public bool HasChartForCurrentMode(bool strict = false)
    {
        if (strict)
        {
            return CDTXMania.GetCurrentInstrument() switch
            {
                0 => SongInformation.bScoreExists.Drums,
                1 => SongInformation.bScoreExists.Guitar,
                2 => SongInformation.bScoreExists.Bass,
                _ => false
            };
        }
        
        bool bChartExistForMode = CDTXMania.ConfigIni.bDrumsEnabled && SongInformation.bScoreExists.Drums;
        if (!bChartExistForMode)
        {
            bChartExistForMode = CDTXMania.ConfigIni.bGuitarEnabled &&
                                 (SongInformation.bScoreExists.Guitar || SongInformation.bScoreExists.Bass);
        }
        return bChartExistForMode;
    }

    public void LoadScoreFile()
    {
        string path = FileInformation.AbsoluteFilePath + ".score.ini";
        
		if (!File.Exists(path))
			return;
		
		Trace.TraceInformation("Loading score.ini file: " + path);

		try
		{
			CScoreIni ini = new(path);

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
						SongInformation.BestRank[nInstrumentNumber] =
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
						SongInformation.BestRank[nInstrumentNumber] =
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
					SongInformation.BestRank[nInstrumentNumber] = (int)CScoreIni.ERANK.UNKNOWN;
				}

				//-----------------

				#endregion

				SongInformation.HighCompletionRate[nInstrumentNumber] = ini.stSection[n].dbPerformanceSkill;
				SongInformation.HighSongSkill[nInstrumentNumber] = ini.stSection[n].dbGameSkill;
				SongInformation.FullCombo[nInstrumentNumber] = ini.stSection[n].bIsFullCombo | ini.stSection[nInstrumentNumber * 2].bIsFullCombo;
				
				//New for Progress
				SongInformation.progress[nInstrumentNumber] = ini.stSection[n].strProgress;
				if (SongInformation.progress[nInstrumentNumber] == "")
				{
					//TODO: Read from another file if progress string is empty
					//Set a hard-coded 64 char string for now
					SongInformation.progress[nInstrumentNumber] =
						"0000000000000000000000000000000000000000000000000000000000000000";
				}
			}

			SongInformation.NbPerformances.Drums = ini.stFile.PlayCountDrums;
			SongInformation.NbPerformances.Guitar = ini.stFile.PlayCountGuitar;
			SongInformation.NbPerformances.Bass = ini.stFile.PlayCountBass;
			
			for (int i = 0; i < 5; i++)
				SongInformation.PerformanceHistory[i] = ini.stFile.History[i];
		}
		catch (Exception e)
		{
			Trace.TraceError("Failed to read score.ini file: " + path);
			Trace.TraceError(e.Message);
		}
    }
}