using DTXMania.Core;

namespace DTXMania.SongDb;

public class SortByDifficulty : SongDbSort
{
    public override string Name => "Difficulty";

    public string[] difficultyLabels = ["BASIC", "ADVANCED", "EXTREME", "MASTER", "DTX"];
    
    public override async Task<SongNode> Sort(SongDb songDb)
    {
        //create a new root node
        SongNode root = new(null, SongNode.ENodeType.ROOT);
        
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
            await OrderByDifficulty(difficulty.childNodes);
        }

        return root;
    }

    private async Task OrderByDifficulty(List<SongNode> difficultyChildNodes)
    {
        difficultyChildNodes.Sort((a, b) =>
        {
            //get the first chart for each song
            CScore chartA = a.charts.FirstOrDefault(x => x != null);
            CScore chartB = b.charts.FirstOrDefault(x => x != null);

            if (chartA == null || chartB == null)
            {
                return 0; // skip if no valid chart
            }

            int instrument = (int) a.filteredInstrumentPart;

            //compare by difficulty number
            return chartA.SongInformation.Level[instrument] - chartB.SongInformation.Level[instrument];
        });

        await Task.CompletedTask; // Simulate async operation
    }

    private SongNode CreateDifficultyLabel(string difficultyName, SongNode root)
    {
        SongNode node = new(root, SongNode.ENodeType.BOX)
        {
            title = difficultyName
        };
        return node;
    }
}