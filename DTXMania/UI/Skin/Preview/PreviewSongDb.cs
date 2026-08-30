using DTXMania.Core;
using DTXMania.SongDb;

namespace DTXMania.UI.Skin.Preview;

/// <summary>
/// Fake songs for the skin editor, built as real SongNodes so the sorters and bindings work as normal.
/// Fixed seed, so the list is the same every time.
/// </summary>
internal sealed class PreviewSongDb
{
    private const int Seed = 20260825;
    private const int SongCount = 36;

    private static readonly string[] BoxNames = ["Example Collection", "Another Box", "Nested Favourites"];

    private static readonly string[] Genres =
        ["J-POP", "Rock", "Metal", "Anime", "Jazz", "Electronic", "Classical", ""];

    private static readonly string[] Difficulties = ["BASIC", "ADVANCED", "EXTREME", "MASTER", "ULTIMATE"];

    public SongDb.SongDb Database { get; }

    public PreviewSongDb()
    {
        Random random = new(Seed);

        SongNode root = new(null, SongNode.ENodeType.ROOT);
        List<SongNode> flattened = [];

        SongNode[] boxes = BoxNames.Select(name => new SongNode(root, SongNode.ENodeType.BOX) { title = name }).ToArray();

        //some at the root and some in boxes, so both kinds of row show up
        for (int i = 0; i < SongCount; i++)
        {
            SongNode parent = i % 3 == 0 ? root : boxes[i % boxes.Length];
            SongNode song = BuildSong(parent, i, random);
            flattened.Add(song);
        }

        Database = new SongDb.SongDb(root, flattened);
    }

    private static SongNode BuildSong(SongNode parent, int index, Random random)
    {
        SongNode song = new(parent)
        {
            title = TitleFor(index),
            path = $@"Preview\Song{index + 1}\"
        };

        //0 is allowed: a song with no charts is a real case
        int chartCount = random.Next(0, Difficulties.Length + 1);

        for (int difficulty = 0; difficulty < chartCount; difficulty++)
        {
            song.charts[difficulty] = BuildChart(index, difficulty, random);
            song.difficultyLabel[difficulty] = Difficulties[difficulty];
        }

        song.chartCount = chartCount;
        return song;
    }

    //the awkward cases a skin has to survive
    private static string TitleFor(int index) => index switch
    {
        3 => "Example Song With An Extremely Long Title That Has To Scroll Because It Does Not Fit In The Row",
        7 => "短い曲名",
        _ => $"Example Song {index + 1}"
    };

    private static CChartData BuildChart(int songIndex, int difficulty, Random random)
    {
        CChartData chart = new();

        chart.SongInformation.Title = TitleFor(songIndex);

        //one song with no artist, which the row draws differently
        chart.SongInformation.ArtistName = songIndex == 5 ? string.Empty : $"Example Artist {songIndex + 1}";
        chart.SongInformation.Genre = Genres[songIndex % Genres.Length];
        chart.SongInformation.Comment = $"Preview comment for example song {songIndex + 1}.";
        chart.SongInformation.Bpm = 90.0 + random.Next(0, 130);
        chart.SongInformation.DurationMs = (90 + random.Next(0, 150)) * 1000;

        //one three-digit level, which takes the other branch of GetLevel
        int level = songIndex == 9 && difficulty == 0 ? 250 : 10 + difficulty * 15 + random.Next(0, 15);

        //a difficulty row only draws if the chart has chips for that instrument
        int chips = 250 + random.Next(0, 900);

        //an unplayed chart has no score, so no rank, rate or skill
        bool played = random.Next(0, 3) != 0;

        for (int instrument = 0; instrument < 3; instrument++)
        {
            chart.SongInformation.Level[instrument] = level;
            chart.SongInformation.LevelDec[instrument] = random.Next(0, 100);
            chart.SongInformation.bScoreExists[instrument] = true;
            chart.SongInformation.chipCountByInstrument[instrument] = chips;

            chart.SongInformation.BestRank[instrument] = played
                ? random.Next(0, 7)
                : (int)CScoreIni.ERANK.UNKNOWN;

            chart.SongInformation.HighCompletionRate[instrument] = played ? 40.0 + random.NextDouble() * 60.0 : 0.0;
            chart.SongInformation.FullCombo[instrument] = played && random.Next(0, 5) == 0;
            chart.SongInformation.NbPerformances[instrument] = played ? random.Next(1, 50) : 0;
        }

        //the sorts use the score file to tell played from unplayed
        if (played)
        {
            chart.ScoreIniInformation =
                new CChartData.STScoreIniInformation(DateTime.Now.AddDays(-random.Next(0, 400)), 4096L);
        }

        FillLaneCounts(chart, chips, random);
        return chart;
    }

    //gives the density graph something to draw
    private static void FillLaneCounts(CChartData chart, int chips, Random random)
    {
        ELane[] lanes =
        [
            ELane.LC, ELane.HH, ELane.SD, ELane.BD, ELane.HT, ELane.LT, ELane.FT, ELane.CY,
            ELane.GtR, ELane.GtG, ELane.GtB, ELane.GtY, ELane.GtP, ELane.GtPick,
            ELane.BsR, ELane.BsG, ELane.BsB, ELane.BsY, ELane.BsP, ELane.BsPick
        ];

        int remaining = chips;

        foreach (ELane lane in lanes)
        {
            int share = Math.Min(remaining, random.Next(0, chips / 6));
            chart.SongInformation.chipCountByLane[lane] = share;
            remaining -= share;
        }
    }
}
