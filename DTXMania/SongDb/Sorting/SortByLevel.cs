using System.Diagnostics;
using DTXMania.Core;

namespace DTXMania.SongDb;

public class SortByLevel : SongDbSort
{
    public override string Name => "Level";
    public override string IconName => "level";

    public override Task<SongNode> Sort(SongDb songDb)
    {
        //create a new root node
        SongNode root = new(null, SongNode.ENodeType.ROOT);
        
        Dictionary<int, SongNode> levelNodes = new();
        
        //create boxes for level: Level 1.00~1.49, Level 1.50~1.99, Level 2.00~2.49, etc.
        for (double level = 1.0; level <= 9.99; level += 0.5)
        {
            SongNode node = new(root, SongNode.ENodeType.BOX)
            {
                title = $"Level {level:0.00}~{level + 0.49:0.00}"
            };
            levelNodes[(int)(level * 2)] = node; //level * 2 so we don't need to worry about 0.5
        }

        foreach (var node in songDb.flattenedSongList)
        {
            //sanity check
            if (node.nodeType != SongNode.ENodeType.SONG) continue;

            for (int chartIndex = 0; chartIndex < node.charts.Length; chartIndex++)
            {
                CScore? chart = node.charts[chartIndex];
                if (chart == null) continue;

                for (int instrument = 0; instrument < 3; instrument++)
                {
                    //only process charts for the selected instrument
                    if (CDTXMania.ConfigIni.bDrumsEnabled && instrument != 0) continue;
                    if (!CDTXMania.ConfigIni.bDrumsEnabled && instrument == 0) continue;
                    
                    if (!chart.HasChartForCurrentMode()) continue;

                    //level is from 0-99. our level folders are 0-9.5, with the array being 0-18
                    int level = chart.SongInformation.Level[instrument];
                    double levelDouble = level / 10.0;
                    levelDouble += chart.SongInformation.LevelDec[instrument] / 100.0f;
                    int levelKey = (int)(levelDouble * 2); //multiply by 2 to get the key for the dictionary
                    levelKey = Math.Clamp(levelKey, 2, 19);

                    if (levelNodes.TryGetValue(levelKey, out SongNode? levelNode))
                    {
                        //clone the song node into the level node
                        var newNode = SongNode.Clone(node, levelNode, false);

                        //add charts
                        newNode.filteredInstrumentPart = (EInstrumentPart)instrument;
                        newNode.charts[chartIndex] = chart;
                    }
                    else
                    {
                        Trace.TraceWarning($"Level node not found for level {levelDouble:0.00} in song {node.title}");
                    }
                }
            }
        }
        
        //sort the difficulty nodes by difficulty number
        foreach (SongNode difficulty in root.childNodes)
        {
            OrderByDifficulty(difficulty.childNodes);
        }

        return Task.FromResult(root);
    }
}