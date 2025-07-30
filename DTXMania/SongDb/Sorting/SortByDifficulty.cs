using DTXMania.Core;

namespace DTXMania.SongDb;

public class SortByDifficulty : SongDbSort
{
    public override string Name => "Difficulty";
    public override string IconName => "difficulty";

    public string[] difficultyLabels = ["BASIC", "ADVANCED", "EXTREME", "MASTER", "DTX"];
    
    public override async Task<SongNode> Sort(SongDb songDb)
    {
        //create a new root node
        SongNode root = new(null, SongNode.ENodeType.ROOT)
        {
            title = "Difficulty"
        };

        Dictionary<string, SongNode> difficultyNodes = new();

        foreach (var difficultyLabel in difficultyLabels)
        {
            SongNode node = new(root, SongNode.ENodeType.BOX)
            {
                title = difficultyLabel
            };
            difficultyNodes[difficultyLabel] = node;
        }

        if (!CDTXMania.ConfigIni.bDrumsEnabled)
        {
            //add bass nodes too
            foreach (var difficultyLabel in difficultyLabels)
            {
                string bassDifficultyLabel = "BASS " + difficultyLabel;
                SongNode node = new(root, SongNode.ENodeType.BOX)
                {
                    title = bassDifficultyLabel
                };
                difficultyNodes[bassDifficultyLabel] = node;
            }
        }

        foreach (SongNode songNode in songDb.flattenedSongList)
        {
            for (int index = 0; index < songNode.charts.Length; index++)
            {
                CScore chart = songNode.charts[index];
                
                if (chart == null)
                {
                    continue; // skip if no chart or difficulty name
                }
                
                string difficultyName = difficultyLabels[index];

                if (!chart.HasChartForCurrentMode()) continue;

                if (CDTXMania.ConfigIni.bDrumsEnabled)
                {
                    SongNode difficultyNode = difficultyNodes[difficultyName];
                    SongNode newNode = SongNode.Clone(songNode, difficultyNode, false);
                    newNode.charts[index] = chart;
                    newNode.chartCount = 1;
                    newNode.filteredInstrumentPart = EInstrumentPart.DRUMS;
                }
                else
                {
                    if (chart.SongInformation.bScoreExists.Guitar)
                    {
                        SongNode difficultyNode = difficultyNodes[difficultyName];
                        SongNode newNode = SongNode.Clone(songNode, difficultyNode, false);
                        newNode.charts[index] = chart;
                        newNode.chartCount = 1;
                        newNode.filteredInstrumentPart = EInstrumentPart.GUITAR;
                    }
                    
                    if (chart.SongInformation.bScoreExists.Bass)
                    {
                        difficultyName = "BASS " + difficultyName; // prefix for bass difficulties
                        
                        SongNode difficultyNode = difficultyNodes[difficultyName];
                        SongNode newNode = SongNode.Clone(songNode, difficultyNode, false);
                        newNode.charts[index] = chart;
                        newNode.chartCount = 1;
                        newNode.filteredInstrumentPart = EInstrumentPart.BASS;
                    }
                }
            }
        }
        
        //sort the difficulty nodes by difficulty number
        foreach (SongNode difficulty in root.childNodes)
        {
            OrderByDifficulty(difficulty.childNodes);
        }

        return root;
    }
}