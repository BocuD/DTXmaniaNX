using DTXMania.Core;

namespace DTXMania.SongDb;

public class SortByDifficulty : SongDbSort
{
    public string[] difficultyLabels = ["BASIC", "ADVANCED", "EXTREME", "MASTER", "DTX"];
    
    public override async Task<SongNode> Sort(List<SongNode> flattenedNodes)
    {
        //create a new root node
        SongNode root = new(null)
        {
            nodeType = SongNode.ENodeType.ROOT
        };
        
        Dictionary<string, SongNode> difficultyNodes = new();

        foreach (SongNode songNode in flattenedNodes)
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
                    if (!difficultyNodes.ContainsKey(difficultyName))
                    {
                        SongNode newDifficultyNode = CreateDifficultyLabel(difficultyName, root);
                        difficultyNodes[difficultyName] = newDifficultyNode;
                    }
                    
                    SongNode difficultyNode = difficultyNodes[difficultyName];
                    SongNode newNode = SongNode.Clone(songNode, difficultyNode);
                    newNode.charts[index] = chart;
                    newNode.chartCount = 1;
                    newNode.filteredInstrumentPart = EInstrumentPart.DRUMS;

                    difficultyNode.childNodes.Add(newNode);
                }
                else
                {
                    if (chart.SongInformation.bScoreExists.Guitar)
                    {
                        if (!difficultyNodes.ContainsKey(difficultyName))
                        {
                            SongNode newDifficultyNode = CreateDifficultyLabel(difficultyName, root);
                            difficultyNodes[difficultyName] = newDifficultyNode;
                        }
                        
                        SongNode difficultyNode = difficultyNodes[difficultyName];
                        SongNode newNode = SongNode.Clone(songNode, difficultyNode);
                        newNode.charts[index] = chart;
                        newNode.chartCount = 1;
                        newNode.filteredInstrumentPart = EInstrumentPart.GUITAR;

                        difficultyNode.childNodes.Add(newNode);
                    }
                    
                    if (chart.SongInformation.bScoreExists.Bass)
                    {
                        difficultyName = "BASS " + difficultyName; // prefix for bass difficulties
                        
                        if (!difficultyNodes.ContainsKey(difficultyName))
                        {
                            SongNode newDifficultyNode = CreateDifficultyLabel(difficultyName, root);
                            difficultyNodes[difficultyName] = newDifficultyNode;
                        }
                        
                        SongNode difficultyNode = difficultyNodes[difficultyName];
                        SongNode newNode = SongNode.Clone(songNode, difficultyNode);
                        newNode.charts[index] = chart;
                        newNode.chartCount = 1;
                        newNode.filteredInstrumentPart = EInstrumentPart.BASS;

                        difficultyNode.childNodes.Add(newNode);
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
        SongNode node = new(root)
        {
            title = difficultyName
        };
        root.childNodes.Add(node);
        return node;
    }
}