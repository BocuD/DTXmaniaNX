using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Cryptography;
using DTXMania.Core;
using FDK;

namespace DTXMania;

public class CScoreIni
{
	// プロパティ

	// [File] セクション
	public STFile stFile;

	[StructLayout(LayoutKind.Sequential)]
	public struct STFile
	{
		public string Title;
		public string Name;
		public string Hash;
		public int PlayCountDrums;
		public int PlayCountGuitar;

		public int PlayCountBass;

		// #23596 10.11.16 add ikanick-----/
		public int ClearCountDrums;
		public int ClearCountGuitar;

		public int ClearCountBass;

		// #24459 2011.2.24 yyagi----------/
		public STDGBVALUE<int> BestRank;

		// --------------------------------/
		public int HistoryCount;
		public string[] History;
		public int BGMAdjust;
	}

	// 演奏記録セクション（9種類）
	public STSection stSection;

	[StructLayout(LayoutKind.Sequential)]
	public struct STSection
	{
		public CPerformanceEntry HiScoreDrums;
		public CPerformanceEntry HiSkillDrums;
		public CPerformanceEntry HiScoreGuitar;
		public CPerformanceEntry HiSkillGuitar;
		public CPerformanceEntry HiScoreBass;
		public CPerformanceEntry HiSkillBass;
		public CPerformanceEntry LastPlayDrums; // #23595 2011.1.9 ikanick
		public CPerformanceEntry LastPlayGuitar; //
		public CPerformanceEntry LastPlayBass; //

		public CPerformanceEntry this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return HiScoreDrums;

					case 1:
						return HiSkillDrums;

					case 2:
						return HiScoreGuitar;

					case 3:
						return HiSkillGuitar;

					case 4:
						return HiScoreBass;

					case 5:
						return HiSkillBass;

					// #23595 2011.1.9 ikanick
					case 6:
						return LastPlayDrums;

					case 7:
						return LastPlayGuitar;

					case 8:
						return LastPlayBass;
					//------------
				}

				throw new IndexOutOfRangeException();
			}
			set
			{
				switch (index)
				{
					case 0:
						HiScoreDrums = value;
						return;

					case 1:
						HiSkillDrums = value;
						return;

					case 2:
						HiScoreGuitar = value;
						return;

					case 3:
						HiSkillGuitar = value;
						return;

					case 4:
						HiScoreBass = value;
						return;

					case 5:
						HiSkillBass = value;
						return;
					// #23595 2011.1.9 ikanick
					case 6:
						LastPlayDrums = value;
						return;

					case 7:
						LastPlayGuitar = value;
						return;

					case 8:
						LastPlayBass = value;
						return;
					//------------------
				}

				throw new IndexOutOfRangeException();
			}
		}
	}

	public enum ESectionType : int
	{
		Unknown = -2,
		File = -1,
		HiScoreDrums = 0,
		HiSkillDrums = 1,
		HiScoreGuitar = 2,
		HiSkillGuitar = 3,
		HiScoreBass = 4,
		HiSkillBass = 5,
		LastPlayDrums = 6, // #23595 2011.1.9 ikanick
		LastPlayGuitar = 7, //
		LastPlayBass = 8, //
	}

	public enum ERANK : int // #24459 yyagi
	{
		SS = 0,
		S = 1,
		A = 2,
		B = 3,
		C = 4,
		D = 5,
		E = 6,
		UNKNOWN = 99
	}

	public class CPerformanceEntry
	{
		public STAUTOPLAY bAutoPlay;
		public bool bDrumsEnabled;
		public bool bGuitarEnabled;
		public STDGBVALUE<bool> bHidden;
		public STDGBVALUE<bool> bLeft;
		public STDGBVALUE<bool> bLight;
		public STDGBVALUE<bool> bReverse;
		public bool bSTAGEFAILEDEnabled;
		public STDGBVALUE<bool> bSudden;
		public bool bTight;
		public bool bMIDIUsed;
		public bool bKeyboardUsed;
		public bool bJoypadUsed;
		public bool bMouseUsed;
		public double dbGameSkill;
		public double dbPerformanceSkill;
		public ECYGroup eCYGroup;
		public EDarkMode eDark;
		public EFTGroup eFTGroup;
		public EHHGroup eHHGroup;
		public EBDGroup eBDGroup;
		public EPlaybackPriority eHitSoundPriorityCY;
		public EPlaybackPriority eHitSoundPriorityFT;
		public EPlaybackPriority eHitSoundPriorityHH;
		public STDGBVALUE<ERandomMode> eRandom;
		public EDamageLevel eDamageLevel;
		public STDGBVALUE<float> fScrollSpeed;

		public string Hash;

		/// <summary>
		/// The primary <see cref="STHitRanges"/> used to achieve the score.
		/// </summary>
		/// <remarks>
		/// For drums, "primary" refers to all non-pedal chips. <br/>
		/// For guitar and bass guitar, this refers to all chips.
		/// </remarks>
		public STHitRanges stPrimaryHitRanges;

		/// <summary>
		/// The secondary <see cref="STHitRanges"/> used to achieve the score.
		/// </summary>
		/// <remarks>
		/// For drums, "secondary" refers to all pedal chips. <br/>
		/// For guitar and bass guitar, this is unused.
		/// </remarks>
		public STHitRanges stSecondaryHitRanges;

		public int nGoodCount;
		public int nGreatCount;
		public int nMissCount;
		public int nPerfectCount;
		public int nPoorCount;
		public int nPerfectCount_ExclAuto;
		public int nGreatCount_ExclAuto;
		public int nGoodCount_ExclAuto;
		public int nPoorCount_ExclAuto;
		public int nMissCount_ExclAuto;
		public long nScore;
		public int nPlaySpeedNumerator;
		public int nPlaySpeedDenominator;
		public int nMaxCombo;
		public int nTotalChipsCount;
		public string strDTXManiaVersion;
		public int nRisky; // #23559 2011.6.20 yyagi 0=OFF, 1-10=Risky
		public string strDateTime;

		//
		public string strProgress;

		public CPerformanceEntry()
		{
			bAutoPlay = new STAUTOPLAY();
			bAutoPlay.LC = false;
			bAutoPlay.HH = false;
			bAutoPlay.SD = false;
			bAutoPlay.BD = false;
			bAutoPlay.HT = false;
			bAutoPlay.LT = false;
			bAutoPlay.FT = false;
			bAutoPlay.CY = false;
			bAutoPlay.LP = false;
			bAutoPlay.LBD = false;
			bAutoPlay.Guitar = false;
			bAutoPlay.Bass = false;
			bAutoPlay.GtR = false;
			bAutoPlay.GtG = false;
			bAutoPlay.GtB = false;
			bAutoPlay.GtY = false;
			bAutoPlay.GtP = false;
			bAutoPlay.GtPick = false;
			bAutoPlay.GtW = false;
			bAutoPlay.BsR = false;
			bAutoPlay.BsG = false;
			bAutoPlay.BsB = false;
			bAutoPlay.BsY = false;
			bAutoPlay.BsP = false;
			bAutoPlay.BsPick = false;
			bAutoPlay.BsW = false;

			bSudden = new STDGBVALUE<bool>();
			bSudden.Drums = false;
			bSudden.Guitar = false;
			bSudden.Bass = false;
			bHidden = new STDGBVALUE<bool>();
			bHidden.Drums = false;
			bHidden.Guitar = false;
			bHidden.Bass = false;
			bReverse = new STDGBVALUE<bool>();
			bReverse.Drums = false;
			bReverse.Guitar = false;
			bReverse.Bass = false;
			eRandom = new STDGBVALUE<ERandomMode>();
			eRandom.Drums = ERandomMode.OFF;
			eRandom.Guitar = ERandomMode.OFF;
			eRandom.Bass = ERandomMode.OFF;
			bLight = new STDGBVALUE<bool>();
			bLight.Drums = false;
			bLight.Guitar = false;
			bLight.Bass = false;
			bLeft = new STDGBVALUE<bool>();
			bLeft.Drums = false;
			bLeft.Guitar = false;
			bLeft.Bass = false;
			fScrollSpeed = new STDGBVALUE<float>();
			fScrollSpeed.Drums = 1f;
			fScrollSpeed.Guitar = 1f;
			fScrollSpeed.Bass = 1f;
			nPlaySpeedNumerator = 20;
			nPlaySpeedDenominator = 20;
			bGuitarEnabled = true;
			bDrumsEnabled = true;
			bSTAGEFAILEDEnabled = true;
			eDamageLevel = EDamageLevel.Normal;
			stPrimaryHitRanges = STHitRanges.tCreateDefaultDTXHitRanges();
			stSecondaryHitRanges = STHitRanges.tCreateDefaultDTXHitRanges();
			strDTXManiaVersion = "Unknown";
			strDateTime = "";
			Hash = "00000000000000000000000000000000";
			strProgress = "";
			nRisky = 0; // #23559 2011.6.20 yyagi
		}


		public bool bIsFullCombo =>
			nMaxCombo > 0 && nMaxCombo == nPerfectCount + nGreatCount + nGoodCount + nPoorCount + nMissCount;

		public bool bHasAnyAutoAtAll =>
			nTotalChipsCount - nPerfectCount_ExclAuto - nGreatCount_ExclAuto - nGoodCount_ExclAuto -
			nPoorCount_ExclAuto - nMissCount_ExclAuto == nTotalChipsCount;
	}

	/// <summary>
	/// <para>.score.ini の存在するフォルダ（絶対パス；末尾に '\' はついていない）。</para>
	/// <para>未保存などでファイル名がない場合は null。</para>
	/// </summary>
	public string iniFileDirectoryName { get; private set; }

	/// <summary>
	/// <para>.score.ini のファイル名（絶対パス）。</para>
	/// <para>未保存などでファイル名がない場合は null。</para>
	/// </summary>
	public string iniFilename { get; private set; }


	/// <summary>
	/// <para>初期化後にiniファイルを読み込むコンストラクタ。</para>
	/// <para>読み込んだiniに不正値があれば、それが含まれるセクションをリセットする。</para>
	/// </summary>
	public CScoreIni(string inputFilePath)
	{
		iniFileDirectoryName = null;
		iniFilename = null;
		stFile = new STFile
		{
			Title = "",
			Name = "",
			Hash = "",
			History = ["", "", "", "", ""],
			BestRank = new STDGBVALUE<int>
			{
				Drums = (int)ERANK.UNKNOWN,
				Guitar = (int)ERANK.UNKNOWN,
				Bass = (int)ERANK.UNKNOWN
			}
		};

		stSection = new STSection
		{
			HiScoreDrums = new CPerformanceEntry(),
			HiSkillDrums = new CPerformanceEntry(),
			HiScoreGuitar = new CPerformanceEntry(),
			HiSkillGuitar = new CPerformanceEntry(),
			HiScoreBass = new CPerformanceEntry(),
			HiSkillBass = new CPerformanceEntry(),
			LastPlayDrums = new CPerformanceEntry(),
			LastPlayGuitar = new CPerformanceEntry(),
			LastPlayBass = new CPerformanceEntry()
		};

		tRead(inputFilePath);
	}

	// メソッド

	/// <summary>
	/// 指定されたファイルの内容から MD5 値を求め、それを16進数に変換した文字列を返す。
	/// </summary>
	/// <param name="filePath">MD5 を求めるファイル名。</param>
	/// <returns>算出結果の MD5 を16進数で並べた文字列。</returns>
	public static string tComputeFileMD5(string filePath)
	{
		FileStream stream = new(filePath, FileMode.Open, FileAccess.Read);
		byte[] buffer = new byte[stream.Length];
		stream.Read(buffer, 0, (int)stream.Length);
		stream.Close();
		StringBuilder builder = new(33);
		{
			MD5CryptoServiceProvider m = new();
			byte[] buffer2 = m.ComputeHash(buffer);
			foreach (byte num in buffer2)
				builder.Append(num.ToString("x2"));
		}
		return builder.ToString();
	}

	/// <summary>
	/// 指定された .score.ini を読み込む。内容の真偽は判定しない。
	/// </summary>
	/// <param name="iniFilePath">読み込む .score.ini ファイルを指定します（絶対パスが安全）。</param>
	private void tRead(string iniFilePath)
	{
		iniFileDirectoryName = Path.GetDirectoryName(iniFilePath);
		iniFilename = Path.GetFileName(iniFilePath);

		ESectionType section = ESectionType.Unknown;
		if (!File.Exists(iniFilePath)) return;

		StreamReader reader = new(iniFilePath, Encoding.GetEncoding("shift-jis"));
		CPerformanceEntry entry = new();

		while (reader.ReadLine() is { } str)
		{
			str = str.Replace('\t', ' ').TrimStart('\t', ' ');
			if (str.Length == 0 || str[0] == ';') continue;

			if (!ReadLine(iniFilePath, str, ref section, ref entry)) break;
		}

		reader.Close();
	}

	private bool ReadLine(string iniFilePath, string str, ref ESectionType section, ref CPerformanceEntry cPerformanceEntry)
	{
		try
		{
			#region [ section ]
			if (str[0] == '[')
			{
				
				StringBuilder builder = new(0x20);
				int num = 1;
				while (num < str.Length && str[num] != ']')
				{
					builder.Append(str[num++]);
				}

				string sectionString = builder.ToString();
				section = sectionString switch
				{
					"File" => ESectionType.File,
					"HiScore.Drums" => ESectionType.HiScoreDrums,
					"HiSkill.Drums" => ESectionType.HiSkillDrums,
					"HiScore.Guitar" => ESectionType.HiScoreGuitar,
					"HiSkill.Guitar" => ESectionType.HiSkillGuitar,
					"HiScore.Bass" => ESectionType.HiScoreBass,
					"HiSkill.Bass" => ESectionType.HiSkillBass,
					"LastPlay.Drums" => ESectionType.LastPlayDrums,
					"LastPlay.Guitar" => ESectionType.LastPlayGuitar,
					"LastPlay.Bass" => ESectionType.LastPlayBass,
					_ => ESectionType.Unknown
				};
			}
			#endregion
			else
			{
				string[] strArray = str.Split(['=']);
				if (strArray.Length != 2) return true;
				
				string item = strArray[0].Trim();
				string param = strArray[1].Trim();

				switch (section)
				{
					case ESectionType.File:
					{
						HandleFileSectionLine(item, param);
						break;
					}
					case ESectionType.HiScoreDrums:
					case ESectionType.HiSkillDrums:
					case ESectionType.HiScoreGuitar:
					case ESectionType.HiSkillGuitar:
					case ESectionType.HiScoreBass:
					case ESectionType.HiSkillBass:
					case ESectionType.LastPlayDrums: // #23595 2011.1.9 ikanick
					case ESectionType.LastPlayGuitar:
					case ESectionType.LastPlayBass:
					{
						cPerformanceEntry = stSection[(int)section];
						HandleScoreSectionLine(cPerformanceEntry, item, param);
						break;
					}
				}
			}
			return true;
		}
		catch (Exception exception)
		{
			Trace.TraceError("{0}読み込みを中断します。({1})", exception.Message, iniFilePath);
			return false;
		}
	}

	private void HandleFileSectionLine(string item, string param)
	{
		switch (item)
		{
			case "Title":
				stFile.Title = param;
				break;
				
			case "Name":
				stFile.Name = param;
				break;
				
			case "Hash":
				stFile.Hash = param;
				break;
				
			case "PlayCountDrums":
				stFile.PlayCountDrums = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
				break;
				
			// #23596 11.2.5 changed ikanick
			case "PlayCountGuitars":
				stFile.PlayCountGuitar = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
				break;
			case "PlayCountBass":
				stFile.PlayCountBass = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
				break;
				
			// #23596 10.11.16 add ikanick------------------------------------/
			case "ClearCountDrums":
				stFile.ClearCountDrums = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
				break;
				
			// #23596 11.2.5 changed ikanick
			case "ClearCountGuitars":
				stFile.ClearCountGuitar = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
				break;
				
			case "ClearCountBass":
				stFile.ClearCountBass = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
				break;
				
			// #24459 2011.2.24 yyagi-----------------------------------------/
			case "BestRankDrums":
				stFile.BestRank.Drums = CConversion.nGetNumberIfInRange(param, (int)ERANK.SS, (int)ERANK.E, (int)ERANK.UNKNOWN);
				break;
				
			case "BestRankGuitar":
				stFile.BestRank.Guitar = CConversion.nGetNumberIfInRange(param, (int)ERANK.SS, (int)ERANK.E, (int)ERANK.UNKNOWN);
				break;
				
			case "BestRankBass":
				stFile.BestRank.Bass = CConversion.nGetNumberIfInRange(param, (int)ERANK.SS, (int)ERANK.E, (int)ERANK.UNKNOWN);
				break;
				
			case "BGMAdjust":
				stFile.BGMAdjust = CConversion.nStringToInt(param, 0);
				break;
				
			default:
				if (item.StartsWith("History"))
				{
					if (item.Equals("HistoryCount"))
					{
						stFile.HistoryCount = CConversion.nGetNumberIfInRange(param, 0, 99999999, 0);
					}
					//if there is a number after "History"
					else if (int.TryParse(item[7..], out int index))
					{
						if (index >= 0)
						{
							if (index >= stFile.History.Length)
							{
								Array.Resize(ref stFile.History, index + 1);
							}
								
							stFile.History[index] = param;
						}
					}
				}
				break;
		}
	}

	private void HandleScoreSectionLine(CPerformanceEntry cPerformanceEntry, string item, string param)
	{
		switch (item)
		{
			case "Score":
				cPerformanceEntry.nScore = long.Parse(param);
				break;
				
			case "PlaySkill":
				cPerformanceEntry.dbPerformanceSkill = (double)decimal.Parse(param);
				break;
				
			case "Skill":
				cPerformanceEntry.dbGameSkill = (double)decimal.Parse(param);
				break;
				
			case "Perfect":
				cPerformanceEntry.nPerfectCount = int.Parse(param);
				break;
				
			case "Great":
				cPerformanceEntry.nGreatCount = int.Parse(param);
				break;
				
			case "Good":
				cPerformanceEntry.nGoodCount = int.Parse(param);
				break;
				
			case "Poor":
				cPerformanceEntry.nPoorCount = int.Parse(param);
				break;
				
			case "Miss":
				cPerformanceEntry.nMissCount = int.Parse(param);
				break;
				
			case "MaxCombo":
				cPerformanceEntry.nMaxCombo = int.Parse(param);
				break;
				
			case "TotalChips":
				cPerformanceEntry.nTotalChipsCount = int.Parse(param);
				break;
				
			case "AutoPlay":
			{
				// LCなし               LCあり               CYとRDが別           Gt/Bs autolane/pick
				if (param.Length is 9 or 10 or 11 or 21)
				{
					for (int i = 0; i < param.Length; i++)
					{
						cPerformanceEntry.bAutoPlay[i] = ONorOFF(param[i]);
					}
				}
				break;
			}
				
			case "Risky":
				cPerformanceEntry.nRisky = int.Parse(param);
				break;
				
			case "TightDrums":
				cPerformanceEntry.bTight = CConversion.bONorOFF(param[0]);
				break;
				
			case "SuddenDrums":
				cPerformanceEntry.bSudden.Drums = CConversion.bONorOFF(param[0]);
				break;
				
			case "SuddenGuitar":
				cPerformanceEntry.bSudden.Guitar = CConversion.bONorOFF(param[0]);
				break;
				
			case "SuddenBass":
				cPerformanceEntry.bSudden.Bass = CConversion.bONorOFF(param[0]);
				break;
				
			case "HiddenDrums":
				cPerformanceEntry.bHidden.Drums = CConversion.bONorOFF(param[0]);
				break;
				
			case "HiddenGuitar":
				cPerformanceEntry.bHidden.Guitar = CConversion.bONorOFF(param[0]);
				break;
				
			case "HiddenBass":
				cPerformanceEntry.bHidden.Bass = CConversion.bONorOFF(param[0]);
				break;
				
			case "ReverseDrums":
				cPerformanceEntry.bReverse.Drums = CConversion.bONorOFF(param[0]);
				break;
				
			case "ReverseGuitar":
				cPerformanceEntry.bReverse.Guitar = CConversion.bONorOFF(param[0]);
				break;
				
			case "ReverseBass":
				cPerformanceEntry.bReverse.Bass = CConversion.bONorOFF(param[0]);
				break;
				
			case "RandomGuitar":
				switch (int.Parse(param))
				{
					case (int)ERandomMode.OFF:
					{
						cPerformanceEntry.eRandom.Guitar = ERandomMode.OFF;
						break;
					}
					case (int)ERandomMode.RANDOM:
					{
						cPerformanceEntry.eRandom.Guitar = ERandomMode.RANDOM;
						break;
					}
					case (int)ERandomMode.SUPERRANDOM:
					{
						cPerformanceEntry.eRandom.Guitar = ERandomMode.SUPERRANDOM;
						break;
					}
					case (int)ERandomMode.HYPERRANDOM: // #25452 2011.6.20 yyagi
					{
						cPerformanceEntry.eRandom.Guitar = ERandomMode.HYPERRANDOM;
						break;
					}
					default:
						throw new Exception("RandomGuitar の値が無効です。");
				}
				break;

			case "RandomBass":
				switch (int.Parse(param))
				{
					case (int)ERandomMode.OFF:
					{
						cPerformanceEntry.eRandom.Bass = ERandomMode.OFF;
						break;
					}
					case (int)ERandomMode.RANDOM:
					{
						cPerformanceEntry.eRandom.Bass = ERandomMode.RANDOM;
						break;
					}
					case (int)ERandomMode.SUPERRANDOM:
					{
						cPerformanceEntry.eRandom.Bass = ERandomMode.SUPERRANDOM;
						break;
					}
					case (int)ERandomMode.HYPERRANDOM: // #25452 2011.6.20 yyagi
					{
						cPerformanceEntry.eRandom.Bass = ERandomMode.HYPERRANDOM;
						break;
					}
					default:
						throw new Exception("RandomBass の値が無効です。");
				}
				break;

			case "LightGuitar":
				cPerformanceEntry.bLight.Guitar = CConversion.bONorOFF(param[0]);
				break;

			case "LightBass":
				cPerformanceEntry.bLight.Bass = CConversion.bONorOFF(param[0]);
				break;

			case "LeftGuitar":
				cPerformanceEntry.bLeft.Guitar = CConversion.bONorOFF(param[0]);
				break;

			case "LeftBass":
				cPerformanceEntry.bLeft.Bass = CConversion.bONorOFF(param[0]);
				break;

			case "Dark":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eDark = EDarkMode.OFF;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eDark = EDarkMode.HALF;
						break;
					}
					case 2:
					{
						cPerformanceEntry.eDark = EDarkMode.FULL;
						break;
					}
					default:
						throw new Exception("Dark の値が無効です。");
				}
				break;
				
			case "ScrollSpeedDrums":
				cPerformanceEntry.fScrollSpeed.Drums = (float)decimal.Parse(param);
				break;
				
			case "ScrollSpeedGuitar":
				cPerformanceEntry.fScrollSpeed.Guitar = (float)decimal.Parse(param);
				break;
				
			case "ScrollSpeedBass":
				cPerformanceEntry.fScrollSpeed.Bass = (float)decimal.Parse(param);
				break;
				
			case "PlaySpeed":
			{
				string[] strArray2 = param.Split(['/']);
				if (strArray2.Length == 2)
				{
					cPerformanceEntry.nPlaySpeedNumerator = int.Parse(strArray2[0]);
					cPerformanceEntry.nPlaySpeedDenominator = int.Parse(strArray2[1]);
				}
				break;
			}
				
			case "HHGroup":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eHHGroup = EHHGroup.全部打ち分ける;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eHHGroup = EHHGroup.ハイハットのみ打ち分ける;
						break;
					}
					case 2:
					{
						cPerformanceEntry.eHHGroup = EHHGroup.左シンバルのみ打ち分ける;
						break;
					}
					case 3:
					{
						cPerformanceEntry.eHHGroup = EHHGroup.全部共通;
						break;
					}
					default:
						throw new Exception("HHGroup の値が無効です。");
				}
				break;

			case "FTGroup":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eFTGroup = EFTGroup.打ち分ける;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eFTGroup = EFTGroup.共通;
						break;
					}
					default:
						throw new Exception("FTGroup の値が無効です。");
				}
				break;

			case "CYGroup":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eCYGroup = ECYGroup.打ち分ける;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eCYGroup = ECYGroup.共通;
						break;
					}
					default:
						throw new Exception("CYGroup の値が無効です。");
				}
				break;

			case "BDGroup":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eBDGroup = EBDGroup.打ち分ける;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eBDGroup = EBDGroup.左右ペダルのみ打ち分ける;
						break;
					}
					case 2:
					{
						cPerformanceEntry.eBDGroup = EBDGroup.どっちもBD;
						break;
					}
					default:
						throw new Exception("HHGroup の値が無効です。");
				}

				break;

			case "HitSoundPriorityHH":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eHitSoundPriorityHH =
							EPlaybackPriority.ChipOverPadPriority;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eHitSoundPriorityHH =
							EPlaybackPriority.PadOverChipPriority;
						break;
					}
					default:
						throw new Exception("HitSoundPriorityHH の値が無効です。");
				}
				break;

			case "HitSoundPriorityFT":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eHitSoundPriorityFT =
							EPlaybackPriority.ChipOverPadPriority;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eHitSoundPriorityFT =
							EPlaybackPriority.PadOverChipPriority;
						break;
					}
					default:
						throw new Exception("HitSoundPriorityFT の値が無効です。");
				}
				break;

			case "HitSoundPriorityCY":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eHitSoundPriorityCY =
							EPlaybackPriority.ChipOverPadPriority;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eHitSoundPriorityCY =
							EPlaybackPriority.PadOverChipPriority;
						break;
					}
					default:
						throw new Exception("HitSoundPriorityCY の値が無効です。");
				}
				break;

			case "Guitar":
				cPerformanceEntry.bGuitarEnabled = CConversion.bONorOFF(param[0]);
				break;

			case "Drums":
				cPerformanceEntry.bDrumsEnabled = CConversion.bONorOFF(param[0]);
				break;

			case "StageFailed":
				cPerformanceEntry.bSTAGEFAILEDEnabled = CConversion.bONorOFF(param[0]);
				break;

			case "DamageLevel":
				switch (int.Parse(param))
				{
					case 0:
					{
						cPerformanceEntry.eDamageLevel = EDamageLevel.Small;
						break;
					}
					case 1:
					{
						cPerformanceEntry.eDamageLevel = EDamageLevel.Normal;
						break;
					}
					case 2:
					{
						cPerformanceEntry.eDamageLevel = EDamageLevel.High;
						break;
					}
					default:
						throw new Exception("DamageLevel の値が無効です。");
				}
				break;

			case "UseKeyboard":
				cPerformanceEntry.bKeyboardUsed = CConversion.bONorOFF(param[0]);
				break;

			case "UseMIDIIN":
				cPerformanceEntry.bMIDIUsed = CConversion.bONorOFF(param[0]);
				break;

			case "UseJoypad":
				cPerformanceEntry.bJoypadUsed = CConversion.bONorOFF(param[0]);
				break;

			case "UseMouse":
				cPerformanceEntry.bMouseUsed = CConversion.bONorOFF(param[0]);
				break;

			case "DTXManiaVersion":
				cPerformanceEntry.strDTXManiaVersion = param;
				break;

			case "DateTime":
				cPerformanceEntry.strDateTime = param;
				break;

			case "Progress":
				cPerformanceEntry.strProgress = param;
				break;

			case "Hash":
				cPerformanceEntry.Hash = param;
				break;

			case "9LaneMode":
				CConversion.bONorOFF(param[0]);
				break;

			default:
			{
				if (!int.TryParse(param, out int nValue))
				{
					break;
				}

				switch (item)
				{
					// legacy hit ranges
					// map legacy hit ranges to both primary and secondary,
					// to emulate the previous behaviour of both being identical

					// legacy perfect range size (±ms)
					case @"PerfectRange":
						cPerformanceEntry.stPrimaryHitRanges.nPerfectSizeMs = nValue;
						cPerformanceEntry.stSecondaryHitRanges.nPerfectSizeMs = nValue;
						break;

					// legacy great range size (±ms)
					case @"GreatRange":
						cPerformanceEntry.stPrimaryHitRanges.nGreatSizeMs = nValue;
						cPerformanceEntry.stSecondaryHitRanges.nGreatSizeMs = nValue;
						break;

					// legacy good range size (±ms)
					case @"GoodRange":
						cPerformanceEntry.stPrimaryHitRanges.nGoodSizeMs = nValue;
						cPerformanceEntry.stSecondaryHitRanges.nGoodSizeMs = nValue;
						break;

					// legacy poor range size (±ms)
					case @"PoorRange":
						cPerformanceEntry.stPrimaryHitRanges.nPoorSizeMs = nValue;
						cPerformanceEntry.stSecondaryHitRanges.nPoorSizeMs = nValue;
						break;

					// primary hit ranges

					// primary perfect range size (±ms)
					case @"PrimaryPerfectRange":
						cPerformanceEntry.stPrimaryHitRanges.nPerfectSizeMs = nValue;
						break;

					// primary great range size (±ms)
					case @"PrimaryGreatRange":
						cPerformanceEntry.stPrimaryHitRanges.nGreatSizeMs = nValue;
						break;

					// primary good range size (±ms)
					case @"PrimaryGoodRange":
						cPerformanceEntry.stPrimaryHitRanges.nGoodSizeMs = nValue;
						break;

					// primary poor range size (±ms)
					case @"PrimaryPoorRange":
						cPerformanceEntry.stPrimaryHitRanges.nPoorSizeMs = nValue;
						break;

					// secondary hit ranges

					// secondary perfect range size (±ms)
					case @"SecondaryPerfectRange":
						cPerformanceEntry.stSecondaryHitRanges.nPerfectSizeMs = nValue;
						break;

					// secondary great range size (±ms)
					case @"SecondaryGreatRange":
						cPerformanceEntry.stSecondaryHitRanges.nGreatSizeMs = nValue;
						break;

					// secondary good range size (±ms)
					case @"SecondaryGoodRange":
						cPerformanceEntry.stSecondaryHitRanges.nGoodSizeMs = nValue;
						break;

					// secondary poor range size (±ms)
					case @"SecondaryPoorRange":
						cPerformanceEntry.stSecondaryHitRanges.nPoorSizeMs = nValue;
						break;
				}
				break;
			}
		}
	}

	internal void tAddHistory(string str追加文字列)
	{
		stFile.HistoryCount++;
		for (int i = 3; i >= 0; i--)
			stFile.History[i + 1] = stFile.History[i];
		DateTime now = DateTime.Now;
		stFile.History[0] = $"{stFile.HistoryCount:0}.{now.Year % 100:D2}/{now.Month}/{now.Day} {str追加文字列}";
	}

	internal void tExport(string iniFilePath)
	{
		iniFileDirectoryName = Path.GetDirectoryName(iniFilePath);
		iniFilename = Path.GetFileName(iniFilePath);

		StreamWriter writer = new(iniFilePath, false, Encoding.GetEncoding("shift-jis"));
		writer.WriteLine("[File]");
		writer.WriteLine("Title={0}", stFile.Title);
		writer.WriteLine("Name={0}", stFile.Name);
		writer.WriteLine("Hash={0}", stFile.Hash);
		writer.WriteLine("PlayCountDrums={0}", stFile.PlayCountDrums);
		writer.WriteLine("PlayCountGuitars={0}", stFile.PlayCountGuitar);
		writer.WriteLine("PlayCountBass={0}", stFile.PlayCountBass);
		writer.WriteLine("ClearCountDrums={0}", stFile.ClearCountDrums); // #23596 10.11.16 add ikanick
		writer.WriteLine("ClearCountGuitars={0}", stFile.ClearCountGuitar); //
		writer.WriteLine("ClearCountBass={0}", stFile.ClearCountBass); //
		writer.WriteLine("BestRankDrums={0}", stFile.BestRank.Drums); // #24459 2011.2.24 yyagi
		writer.WriteLine("BestRankGuitar={0}", stFile.BestRank.Guitar); //
		writer.WriteLine("BestRankBass={0}", stFile.BestRank.Bass); //
		writer.WriteLine("HistoryCount={0}", stFile.HistoryCount);
		writer.WriteLine("History0={0}", stFile.History[0]);
		writer.WriteLine("History1={0}", stFile.History[1]);
		writer.WriteLine("History2={0}", stFile.History[2]);
		writer.WriteLine("History3={0}", stFile.History[3]);
		writer.WriteLine("History4={0}", stFile.History[4]);
		writer.WriteLine("BGMAdjust={0}", stFile.BGMAdjust);
		writer.WriteLine();
		for (int i = 0; i < 9; i++)
		{
			string[] strArray =
			[
				"HiScore.Drums", "HiSkill.Drums", "HiScore.Guitar", "HiSkill.Guitar", "HiScore.Bass", "HiSkill.Bass",
				"LastPlay.Drums", "LastPlay.Guitar", "LastPlay.Bass"
			];
			writer.WriteLine("[{0}]", strArray[i]);
			writer.WriteLine("Score={0}", stSection[i].nScore);
			writer.WriteLine("PlaySkill={0}", stSection[i].dbPerformanceSkill);
			writer.WriteLine("Skill={0}", stSection[i].dbGameSkill);
			writer.WriteLine("Perfect={0}", stSection[i].nPerfectCount);
			writer.WriteLine("Great={0}", stSection[i].nGreatCount);
			writer.WriteLine("Good={0}", stSection[i].nGoodCount);
			writer.WriteLine("Poor={0}", stSection[i].nPoorCount);
			writer.WriteLine("Miss={0}", stSection[i].nMissCount);
			writer.WriteLine("MaxCombo={0}", stSection[i].nMaxCombo);
			writer.WriteLine("TotalChips={0}", stSection[i].nTotalChipsCount);
			writer.Write("AutoPlay=");
			for (int j = 0; j < (int)ELane.MAX; j++)
			{
				writer.Write(stSection[i].bAutoPlay[j] ? 1 : 0);
			}

			writer.WriteLine();
			writer.WriteLine("Risky={0}", stSection[i].nRisky);
			writer.WriteLine("SuddenDrums={0}", stSection[i].bSudden.Drums ? 1 : 0);
			writer.WriteLine("SuddenGuitar={0}", stSection[i].bSudden.Guitar ? 1 : 0);
			writer.WriteLine("SuddenBass={0}", stSection[i].bSudden.Bass ? 1 : 0);
			writer.WriteLine("HiddenDrums={0}", stSection[i].bHidden.Drums ? 1 : 0);
			writer.WriteLine("HiddenGuitar={0}", stSection[i].bHidden.Guitar ? 1 : 0);
			writer.WriteLine("HiddenBass={0}", stSection[i].bHidden.Bass ? 1 : 0);
			writer.WriteLine("ReverseDrums={0}", stSection[i].bReverse.Drums ? 1 : 0);
			writer.WriteLine("ReverseGuitar={0}", stSection[i].bReverse.Guitar ? 1 : 0);
			writer.WriteLine("ReverseBass={0}", stSection[i].bReverse.Bass ? 1 : 0);
			writer.WriteLine("TightDrums={0}", stSection[i].bTight ? 1 : 0);
			writer.WriteLine("RandomGuitar={0}", (int)stSection[i].eRandom.Guitar);
			writer.WriteLine("RandomBass={0}", (int)stSection[i].eRandom.Bass);
			writer.WriteLine("LightGuitar={0}", stSection[i].bLight.Guitar ? 1 : 0);
			writer.WriteLine("LightBass={0}", stSection[i].bLight.Bass ? 1 : 0);
			writer.WriteLine("LeftGuitar={0}", stSection[i].bLeft.Guitar ? 1 : 0);
			writer.WriteLine("LeftBass={0}", stSection[i].bLeft.Bass ? 1 : 0);
			writer.WriteLine("Dark={0}", (int)stSection[i].eDark);
			writer.WriteLine("ScrollSpeedDrums={0}", stSection[i].fScrollSpeed.Drums);
			writer.WriteLine("ScrollSpeedGuitar={0}", stSection[i].fScrollSpeed.Guitar);
			writer.WriteLine("ScrollSpeedBass={0}", stSection[i].fScrollSpeed.Bass);
			writer.WriteLine("PlaySpeed={0}/{1}", stSection[i].nPlaySpeedNumerator, stSection[i].nPlaySpeedDenominator);
			writer.WriteLine("HHGroup={0}", (int)stSection[i].eHHGroup);
			writer.WriteLine("FTGroup={0}", (int)stSection[i].eFTGroup);
			writer.WriteLine("CYGroup={0}", (int)stSection[i].eCYGroup);
			writer.WriteLine("BDGroup={0}", (int)stSection[i].eBDGroup);
			writer.WriteLine("HitSoundPriorityHH={0}", (int)stSection[i].eHitSoundPriorityHH);
			writer.WriteLine("HitSoundPriorityFT={0}", (int)stSection[i].eHitSoundPriorityFT);
			writer.WriteLine("HitSoundPriorityCY={0}", (int)stSection[i].eHitSoundPriorityCY);
			writer.WriteLine("Guitar={0}", stSection[i].bGuitarEnabled ? 1 : 0);
			writer.WriteLine("Drums={0}", stSection[i].bDrumsEnabled ? 1 : 0);
			writer.WriteLine("StageFailed={0}", stSection[i].bSTAGEFAILEDEnabled ? 1 : 0);
			writer.WriteLine("DamageLevel={0}", (int)stSection[i].eDamageLevel);
			writer.WriteLine("UseKeyboard={0}", stSection[i].bKeyboardUsed ? 1 : 0);
			writer.WriteLine("UseMIDIIN={0}", stSection[i].bMIDIUsed ? 1 : 0);
			writer.WriteLine("UseJoypad={0}", stSection[i].bJoypadUsed ? 1 : 0);
			writer.WriteLine("UseMouse={0}", stSection[i].bMouseUsed ? 1 : 0);
			writer.WriteLine($@"PrimaryPerfectRange={stSection[i].stPrimaryHitRanges.nPerfectSizeMs}");
			writer.WriteLine($@"PrimaryGreatRange={stSection[i].stPrimaryHitRanges.nGreatSizeMs}");
			writer.WriteLine($@"PrimaryGoodRange={stSection[i].stPrimaryHitRanges.nGoodSizeMs}");
			writer.WriteLine($@"PrimaryPoorRange={stSection[i].stPrimaryHitRanges.nPoorSizeMs}");
			writer.WriteLine($@"SecondaryPerfectRange={stSection[i].stSecondaryHitRanges.nPerfectSizeMs}");
			writer.WriteLine($@"SecondaryGreatRange={stSection[i].stSecondaryHitRanges.nGreatSizeMs}");
			writer.WriteLine($@"SecondaryGoodRange={stSection[i].stSecondaryHitRanges.nGoodSizeMs}");
			writer.WriteLine($@"SecondaryPoorRange={stSection[i].stSecondaryHitRanges.nPoorSizeMs}");
			writer.WriteLine("DTXManiaVersion={0}", stSection[i].strDTXManiaVersion);
			writer.WriteLine("DateTime={0}", stSection[i].strDateTime);
			writer.WriteLine("Progress={0}", stSection[i].strProgress);
			writer.WriteLine("Hash={0}", stSection[i].Hash);
		}

		writer.Close();
	}

	internal static int tCalculateRank(CPerformanceEntry part)
	{
		if (part.bMIDIUsed || part.bKeyboardUsed || part.bJoypadUsed || part.bMouseUsed)	// 2010.9.11
		{
			int nTotal = part.nPerfectCount + part.nGreatCount + part.nGoodCount + part.nPoorCount + part.nMissCount;
			return tCalculateRank(nTotal, part.nPerfectCount, part.nGreatCount, part.nGoodCount, part.nPoorCount, part.nMissCount, part.nMaxCombo);
		}
		return (int)ERANK.UNKNOWN;
	}

	/*
	 Compare 2 progress bars for Stage Failed only
	 */
	internal static bool tCheckIfUpdateProgressBarRecordOrNot(string strBestProgress, string strCurrProgress) 
	{
		bool ret = false;
		//Current record is invalid
		if (strCurrProgress.Length != CActPerfProgressBar.nSectionIntervalCount)
		{
			return false;
		}

		//Best Progress record does not exist
		if(strBestProgress.Length != CActPerfProgressBar.nSectionIntervalCount && 
		   strCurrProgress.Length == CActPerfProgressBar.nSectionIntervalCount)
		{
			return true;
		}

		int nBestProgressLength = tProgressBarLength(strBestProgress);
		int nCurrProgressLength = tProgressBarLength(strCurrProgress);

		//If Best record is a clear, progress record is updated only based on skill for now 
		if(nBestProgressLength == CActPerfProgressBar.nSectionIntervalCount)
		{
			return false;
		}

		//
		if(nCurrProgressLength >= nBestProgressLength)
		{
			ret = true;
		}

		return ret;
			
	}
        
	internal static int tProgressBarLength(string strProgressBar)
	{
		if (strProgressBar == null || strProgressBar.Length != CActPerfProgressBar.nSectionIntervalCount)
		{
			return 0;
		}

		char[] arrCurrProgress = strProgressBar.ToCharArray();
		int nCurrProgressLength = 0;
		for (int i = 0; i < arrCurrProgress.Length; i++)
		{
			if (arrCurrProgress[i] == '0')
			{
				break;
			}
			nCurrProgressLength++;
		}
		return nCurrProgressLength;
	}

	/// <summary>
	/// nDummy 適当な数値を入れてください。特に使いません。
	/// dRate 達成率を入れます。
	/// </summary>
	internal static int tCalculateRank(double completionRate)
	{
		return completionRate switch
		{
			0 => (int)ERANK.UNKNOWN,
			>= 95 => (int)ERANK.SS,
			>= 80 => (int)ERANK.S,
			>= 73 => (int)ERANK.A,
			>= 63 => (int)ERANK.B,
			>= 53 => (int)ERANK.C,
			>= 45 => (int)ERANK.D,
			_ => (int)ERANK.E
		};
	}

	internal static int tCalculateRank(int nTotal, int nPerfect, int nGreat, int nGood, int nPoor, int nMiss, int nCombo)
	{
		if (nTotal <= 0)
			return (int)ERANK.UNKNOWN;

		//int nRank = (int)ERANK.E;
		int nAuto = nTotal - (nPerfect + nGreat + nGood + nPoor + nMiss);
		if (nTotal <= nAuto)
		{
			return (int)ERANK.SS;
		}

		// Remark: this rate uses the percentage of perfect, great and combo compared to the number of non-auto chips only
		// while the official rate from tCalculatePlayingSkill uses the percentage compared to the full total number of chips
		// So this is probably wrong, but I'm not touching it for now.
		double dRate = 100.0 * nPerfect / (nTotal - nAuto) * 0.85 + 100.0 * nGreat / (nTotal - nAuto) * 0.35 + 100.0 * nCombo / (nTotal - nAuto) * 0.15;

		return dRate switch
		{
			>= 95 => (int)ERANK.SS,
			>= 80 => (int)ERANK.S,
			>= 73 => (int)ERANK.A,
			>= 63 => (int)ERANK.B,
			>= 53 => (int)ERANK.C,
			>= 45 => (int)ERANK.D,
			_ => (int)ERANK.E
		};
	}
	internal static double tCalculateGameSkill(double dbLevel, int nLevelDec, int nTotal, int nPerfect, int nGreat, int nGood, int nPoor, int nMiss, int nCombo, EInstrumentPart inst, STAUTOPLAY bAutoPlay)
	{
		//こちらはプレイヤースキル_全曲スキルに加算される得点。いわゆる曲別スキル。

		double dbRate = tCalculatePlayingSkill(nTotal, nPerfect, nGreat, nCombo, nPoor, nMiss, nCombo, inst, bAutoPlay);

		double ret = tCalculateGameSkillFromPlayingSkill(dbLevel, nLevelDec, dbRate);

		return ret;
	}
	internal static double tCalculateGameSkillFromPlayingSkill(double dbLevel, int nLevelDec, double dbPlayingSkill, bool bLivePlay = true)
	{
		if (dbLevel >= 100)
		{
			dbLevel = dbLevel / 100.0;
		}
		else if (dbLevel < 100)
		{
			dbLevel = dbLevel / 10.0 + nLevelDec / 100.0;
		}

		if (bLivePlay && CDTXMania.ConfigIni.bDrumsEnabled && CDTXMania.ConfigIni.bAllDrumsAreAutoPlay)
		{
			return 0;
		}

		return dbPlayingSkill * dbLevel * 0.2;
	}
	internal static double tCalculatePlayingSkill(int nTotal, int nPerfect, int nGreat, int nGood, int nPoor, int nMiss, int nCombo, EInstrumentPart inst, STAUTOPLAY bAutoPlay)
	{
		if (nTotal == 0)
			return 0.0;

		int nAuto = nTotal - (nPerfect + nGreat + nGood + nPoor + nMiss);
		double dbPERFECT率 = 100.0 * nPerfect / nTotal;
		double dbGREAT率 = 100.0 * nGreat / nTotal;
		double dbCOMBO率 = 100.0 * nCombo / nTotal;

		if (nTotal == nAuto)
		{
			dbCOMBO率 = 0.0;
		}

		double ret = dbPERFECT率 * 0.85 + dbGREAT率 * 0.35 + dbCOMBO率 * 0.15;
		ret *= dbCalcReviseValForDrGtBsAutoLanes(inst, bAutoPlay);
		return ret;
	}
	internal static double tCalculateGhostSkill(int nTotal, int nPerfect, int nCombo, EInstrumentPart inst)
	{
		if (nTotal == 0)
			return 0.0;

		double dbPERFECT率 = 100.0 * nPerfect / nTotal;
		double dbGREAT率 = 100.0 * nPerfect / nTotal;
		double dbCOMBO率 = 100.0 * nCombo / nTotal;

		double ret = dbPERFECT率 * 0.85 + dbGREAT率 * 0.35 + dbCOMBO率 * 0.15;

		return ret;
	}
	internal static int tCalculateRankOld(CPerformanceEntry part)
	{
		if (part.bMIDIUsed || part.bKeyboardUsed || part.bJoypadUsed || part.bMouseUsed)	// 2010.9.11
		{
			int nTotal = part.nPerfectCount + part.nGreatCount + part.nGoodCount + part.nPoorCount + part.nMissCount;
			return tCalculateRankOld(nTotal, part.nPerfectCount, part.nGreatCount, part.nGoodCount, part.nPoorCount, part.nMissCount);
		}
		return (int)ERANK.UNKNOWN;
	}
	internal static int tCalculateRankOld(int nTotal, int nPerfect, int nGreat, int nGood, int nPoor, int nMiss)
	{
		if (nTotal <= 0)
			return (int)ERANK.UNKNOWN;

		//int nRank = (int)ERANK.E;
		int nAuto = nTotal - (nPerfect + nGreat + nGood + nPoor + nMiss);
		if (nTotal == nAuto)
		{
			return (int)ERANK.SS;
		}
		double dRate = (double)(nPerfect + nGreat) / (double)(nTotal - nAuto);
		if (dRate == 1.0)
		{
			return (int)ERANK.SS;
		}
		if (dRate >= 0.95)
		{
			return (int)ERANK.S;
		}
		if (dRate >= 0.9)
		{
			return (int)ERANK.A;
		}
		if (dRate >= 0.85)
		{
			return (int)ERANK.B;
		}
		if (dRate >= 0.8)
		{
			return (int)ERANK.C;
		}
		if (dRate >= 0.7)
		{
			return (int)ERANK.D;
		}
		return (int)ERANK.E;
	}
	internal static double tCalculateGameSkillOld( double dbLevel, int nLevelDec, int nTotal, int nPerfect, int nGreat, int nCombo, EInstrumentPart inst, STAUTOPLAY bAutoPlay )
	{
		double ret;
		double rate = 0.0;
		if ( nTotal == 0 || ( nPerfect == 0 && nCombo == 0 && nGreat == 0 ) )
			ret = 0.0;

		//Drums: Perfect% x 0.80 + Great% x 0.30 + Combo% + 0.20 (percents as decimals)
		//Guitar: Perfect% x 0.80 + Great% x 0.20 + Combo% + 0.20 (percents as decimals)
		switch (inst)
		{
			#region [ Unknown ]
			case EInstrumentPart.UNKNOWN:
				throw new ArgumentException();
			#endregion
			#region [ Drums ]
			case EInstrumentPart.DRUMS:
				rate = (nPerfect * 0.8 + nGreat * 0.3 + nCombo * 0.2) / (double)nTotal;
				break;
			#endregion
			#region [ Bass and Guitar ]
			case EInstrumentPart.BASS:
			case EInstrumentPart.GUITAR:
				rate = (nPerfect * 0.8 + nGreat * 0.2 + nCombo * 0.2) / (double)nTotal;
				break;
			#endregion
		}

		//Skill Ratio x Song Level x 0.33 x (0.5 if using Auto-anything, 1 otherwise)
		ret = dbLevel * rate * 0.33;
		ret *= dbCalcReviseValForDrGtBsAutoLanes( inst, bAutoPlay );
		if ( CDTXMania.ConfigIni.bAllDrumsAreAutoPlay )
		{
			return 0;
		}

		return ret;
	}
	internal static double tCalculatePlayingSkillOld(int nTotal, int nPerfect, int nGreat, int nGood, int nPoor, int nMiss, int nCombo, EInstrumentPart inst, STAUTOPLAY bAutoPlay)
	{
		if (nTotal == 0)
			return 0.0;

		//int nAuto = nTotal - (nPerfect + nGreat + nGood + nPoor + nMiss);
		//double y = ((nPerfect * 1.0 + nGreat * 0.8 + nGood * 0.5 + nPoor * 0.2 + nMiss * 0.0 + nAuto * 0.0) * 100.0) / ((double)nTotal);
		//double ret = (100.0 * ((Math.Pow(1.03, y) - 1.0) / (Math.Pow(1.03, 100.0) - 1.0)));
		double ret = 0.0;
		//Drums: Perfect% x 0.80 + Great% x 0.30 + Combo% + 0.20 (percents as decimals)
		//Guitar: Perfect% x 0.80 + Great% x 0.20 + Combo% + 0.20 (percents as decimals)
		switch (inst)
		{
			#region [ Unknown ]
			case EInstrumentPart.UNKNOWN:
				throw new ArgumentException();
			#endregion
			#region [ Drums ]
			case EInstrumentPart.DRUMS:
				ret = (nPerfect * 0.8 + nGreat * 0.3 + nCombo * 0.2) / (double)nTotal * 100.0;
				break;
			#endregion
			#region [ Bass and Guitar ]
			case EInstrumentPart.BASS:
			case EInstrumentPart.GUITAR:
				ret = (nPerfect * 0.8 + nGreat * 0.2 + nCombo * 0.2) / (double)nTotal * 100.0;
				break;
			#endregion
		}

		ret *= dbCalcReviseValForDrGtBsAutoLanes(inst, bAutoPlay);
		return ret;
	}
	internal static double tCalculateGhostSkillOld(int nTotal, int nPerfect, int nGreat, int nGood, int nPoor, int nMiss, int nCombo, EInstrumentPart inst)
	{
		if (nTotal == 0)
			return 0.0;
		//int nAuto = nTotal - (nPerfect + nGreat + nGood + nPoor + nMiss);
		//double y = ((nPerfect * 1.0 + nGreat * 0.8 + nGood * 0.5 + nPoor * 0.2 + nMiss * 0.0 + nAuto * 0.0) * 100.0) / ((double)nTotal);
		//double ret = (100.0 * ((Math.Pow(1.03, y) - 1.0) / (Math.Pow(1.03, 100.0) - 1.0)));
		double ret = 0.0;
		switch (inst)
		{
			#region [ Unknown ]
			case EInstrumentPart.UNKNOWN:
				throw new ArgumentException();
			#endregion
			#region [ Drums ]
			case EInstrumentPart.DRUMS:
				ret = (nPerfect * 0.8 + nGreat * 0.3 + nCombo * 0.2) / (double)nTotal * 100.0;
				break;
			#endregion
			#region [ Bass and Guitar ]
			case EInstrumentPart.BASS:
			case EInstrumentPart.GUITAR:
				ret = (nPerfect * 0.8 + nGreat * 0.2 + nCombo * 0.2) / (double)nTotal * 100.0;
				break;
			#endregion
		}

		return ret;
	}
	internal static double dbCalcReviseValForDrGtBsAutoLanes(EInstrumentPart inst, STAUTOPLAY bAutoPlay)	// #28607 2012.6.7 yyagi
	{
		double ret = 1.0;

		switch (inst)
		{
			#region [ Unknown ]
			case EInstrumentPart.UNKNOWN:
				throw new ArgumentException();
			#endregion
			#region [ Drums ]
			case EInstrumentPart.DRUMS:
				if (!CDTXMania.ConfigIni.bAllDrumsAreAutoPlay)
				{
					#region [ Auto BD ]
					if (bAutoPlay.BD && !bAutoPlay.LP && !bAutoPlay.LBD)
					{
						ret /= 2;
					}
					#endregion

					#region [ Auto LP ]
					else if (!bAutoPlay.BD && bAutoPlay.LP || bAutoPlay.LBD)
					{
						ret /= 2;
					}
					#endregion

					#region [ 2Pedal Auto ]
					else if (bAutoPlay.BD && bAutoPlay.LP && bAutoPlay.LBD)
					{
						ret *= 0.25;
					}
					#endregion
				}
				break;
			#endregion
			#region [ Guitar ]
			case EInstrumentPart.GUITAR:
				if (!CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay)
				{
					#region [ Auto Pick ]
					if (bAutoPlay.GtPick)
					{
						ret /= 2;			 // AutoPick時、達成率を1/2にする
					}
					#endregion
					#region [ Auto Neck ]
					int nAutoLanes = 0;
					if (bAutoPlay.GtR)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.GtG)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.GtB)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.GtY)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.GtP)
					{
						nAutoLanes++;
					}
					ret /= Math.Sqrt(nAutoLanes + 1);
					#endregion
				}
				break;
			#endregion
			#region [ Bass ]
			case EInstrumentPart.BASS:
				if (!CDTXMania.ConfigIni.bAllBassAreAutoPlay)
				{
					#region [ Auto Pick ]
					if (bAutoPlay.BsPick)
					{
						ret /= 2;			 // AutoPick時、達成率を1/2にする
					}
					#endregion
					#region [ Auto lanes ]
					int nAutoLanes = 0;
					if (bAutoPlay.BsR)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.BsG)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.BsB)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.BsY)
					{
						nAutoLanes++;
					}
					if (bAutoPlay.BsP)
					{
						nAutoLanes++;
					}
					ret /= Math.Sqrt(nAutoLanes + 1);
					#endregion
				}
				break;
			#endregion
		}
		return ret;
	}
	internal static string tComputePerformanceSectionMD5( CPerformanceEntry cc )
	{
		StringBuilder builder = new();
		builder.Append( cc.nScore.ToString() );
		builder.Append( cc.dbGameSkill.ToString( ".000000" ) );
		builder.Append( cc.dbPerformanceSkill.ToString( ".000000" ) );
		builder.Append( cc.nPerfectCount );
		builder.Append( cc.nGreatCount );
		builder.Append( cc.nGoodCount );
		builder.Append( cc.nPoorCount );
		builder.Append( cc.nMissCount );
		builder.Append( cc.nMaxCombo );
		builder.Append( cc.nTotalChipsCount );
		for( int i = 0; i < 10; i++ )
			builder.Append( boolToChar( cc.bAutoPlay[ i ] ) );
		builder.Append( boolToChar( cc.bTight ) );
		builder.Append( boolToChar( cc.bSudden.Drums ) );
		builder.Append( boolToChar( cc.bSudden.Guitar ) );
		builder.Append( boolToChar( cc.bSudden.Bass ) );
		builder.Append( boolToChar( cc.bHidden.Drums ) );
		builder.Append( boolToChar( cc.bHidden.Guitar ) );
		builder.Append( boolToChar( cc.bHidden.Bass ) );
		builder.Append( boolToChar( cc.bReverse.Drums ) );
		builder.Append( boolToChar( cc.bReverse.Guitar ) );
		builder.Append( boolToChar( cc.bReverse.Bass ) );
		builder.Append( (int) cc.eRandom.Guitar );
		builder.Append( (int) cc.eRandom.Bass );
		builder.Append( boolToChar( cc.bLight.Guitar ) );
		builder.Append( boolToChar( cc.bLight.Bass ) );
		builder.Append( boolToChar( cc.bLeft.Guitar ) );
		builder.Append( boolToChar( cc.bLeft.Bass ) );
		builder.Append( (int) cc.eDark );
		builder.Append( cc.fScrollSpeed.Drums.ToString( ".000000" ) );
		builder.Append( cc.fScrollSpeed.Guitar.ToString( ".000000" ) );
		builder.Append( cc.fScrollSpeed.Bass.ToString( ".000000" ) );
		builder.Append( cc.nPlaySpeedNumerator );
		builder.Append( cc.nPlaySpeedDenominator );
		builder.Append( (int) cc.eHHGroup );
		builder.Append( (int) cc.eFTGroup );
		builder.Append( (int) cc.eCYGroup );
		builder.Append( (int) cc.eHitSoundPriorityHH );
		builder.Append( (int) cc.eHitSoundPriorityFT );
		builder.Append( (int) cc.eHitSoundPriorityCY );
		builder.Append( boolToChar( cc.bGuitarEnabled ) );
		builder.Append( boolToChar( cc.bDrumsEnabled ) );
		builder.Append( boolToChar( cc.bSTAGEFAILEDEnabled ) );
		builder.Append( (int) cc.eDamageLevel );
		builder.Append( boolToChar( cc.bKeyboardUsed ) );
		builder.Append( boolToChar( cc.bMIDIUsed ) );
		builder.Append( boolToChar( cc.bJoypadUsed ) );
		builder.Append( boolToChar( cc.bMouseUsed ) );
		builder.Append(cc.stPrimaryHitRanges.nPerfectSizeMs);
		builder.Append(cc.stPrimaryHitRanges.nGreatSizeMs);
		builder.Append(cc.stPrimaryHitRanges.nGoodSizeMs);
		builder.Append(cc.stPrimaryHitRanges.nPoorSizeMs);
		builder.Append(cc.stSecondaryHitRanges.nPerfectSizeMs);
		builder.Append(cc.stSecondaryHitRanges.nGreatSizeMs);
		builder.Append(cc.stSecondaryHitRanges.nGoodSizeMs);
		builder.Append(cc.stSecondaryHitRanges.nPoorSizeMs);
		builder.Append( cc.strDTXManiaVersion );
		builder.Append( cc.strDateTime );

		byte[] bytes = Encoding.GetEncoding( "shift-jis" ).GetBytes( builder.ToString() );
		StringBuilder builder2 = new(0x21);
		{
			MD5CryptoServiceProvider m = new();
			byte[] buffer2 = m.ComputeHash(bytes);
			foreach (byte num2 in buffer2)
				builder2.Append(num2.ToString("x2"));
		}
		return builder2.ToString();
	}
	
	internal static STDGBVALUE<bool> tGetIsUpdateNeeded()
	{
		STDGBVALUE<bool> result = new()
		{
			Drums = CDTXMania.ConfigIni.bDrumsEnabled && CDTXMania.DTX.bHasChips.Drums && !CDTXMania.ConfigIni.bAllDrumsAreAutoPlay,
			Guitar = CDTXMania.ConfigIni.bGuitarEnabled && CDTXMania.DTX.bHasChips.Guitar && !CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay,
			Bass = CDTXMania.ConfigIni.bGuitarEnabled && CDTXMania.DTX.bHasChips.Bass && !CDTXMania.ConfigIni.bAllBassAreAutoPlay
		};
		return result;
	}
	
	internal static int tCalculateOverallRankValue(CPerformanceEntry Drums, CPerformanceEntry Guitar, CPerformanceEntry Bass)
	{
		int nTotal = Drums.nTotalChipsCount + Guitar.nTotalChipsCount + Bass.nTotalChipsCount;
		int nPerfect = Drums.nPerfectCount_ExclAuto + Guitar.nPerfectCount_ExclAuto + Bass.nPerfectCount_ExclAuto;	// #24569 2011.3.1 yyagi: to calculate result rank without AUTO chips
		int nGreat = Drums.nGreatCount_ExclAuto + Guitar.nGreatCount_ExclAuto + Bass.nGreatCount_ExclAuto;		//
		int nGood = Drums.nGoodCount_ExclAuto + Guitar.nGoodCount_ExclAuto + Bass.nGoodCount_ExclAuto;		//
		int nPoor = Drums.nPoorCount_ExclAuto + Guitar.nPoorCount_ExclAuto + Bass.nPoorCount_ExclAuto;		//
		int nMiss = Drums.nMissCount_ExclAuto + Guitar.nMissCount_ExclAuto + Bass.nMissCount_ExclAuto;		//
		int nCombo = Drums.nMaxCombo + Guitar.nMaxCombo + Bass.nMaxCombo;		//
		if (CDTXMania.ConfigIni.nSkillMode == 0)
		{
			return tCalculateRankOld(nTotal, nPerfect, nGreat, nGood, nPoor, nMiss);
		}
		return tCalculateRank(nTotal, nPerfect, nGreat, nGood, nPoor, nMiss, nCombo);
	}

	// Other

	#region [ private ]
	//-----------------
	private bool ONorOFF( char c )
	{
		return c != '0';
	}
	private static char boolToChar( bool b )
	{
		if( !b )
		{
			return '0';
		}
		return '1';
	}
	//-----------------
	#endregion
}