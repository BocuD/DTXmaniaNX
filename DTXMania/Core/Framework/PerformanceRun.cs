using System.Diagnostics;

namespace DTXMania.Core.Framework;

public static class PerformanceRun
{
    private const double SettleSeconds = 3;

    private const double GiveUpSeconds = 120;

    private static double recordSeconds = 20;

    public static bool Enabled { get; private set; }

    public static void ReadCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i].Equals("--perftest", StringComparison.OrdinalIgnoreCase))
            {
                Enabled = true;
            }
            else if (arguments[i].Equals("--perftest-record", StringComparison.OrdinalIgnoreCase)
                     && i + 1 < arguments.Length
                     && double.TryParse(arguments[i + 1], out double seconds)
                     && seconds > 0)
            {
                recordSeconds = seconds;
            }
        }

        if (Enabled)
        {
            //nothing turns this on by hand during a scripted run, and it is the whole point of one
            FrameProfiler.Enabled = true;
            Trace.TraceInformation($"[perftest] scripted run, recording {recordSeconds}s of play");
        }
    }

    private static StageManager Stages => CDTXMania.StageManager;

    private static IEnumerator<Wait> Script()
    {
        yield return Wait.Until("the title screen", () => Stages.rCurrentStage == Stages.stageTitle);

        Stages.tChangeStage(Stages.stageSongSelectionNew);
        yield return Wait.Until("song select", () => Stages.rCurrentStage == Stages.stageSongSelectionNew);
        yield return Wait.Seconds(SettleSeconds, "the song list to settle");

        if (!Stages.stageSongSelectionNew.ConfirmRandomSong())
        {
            Log("no song to play");
            yield break;
        }

        yield return Wait.Until("the song to start", () => Stages.rCurrentStage is CStagePerfCommonScreen);
        yield return Wait.Seconds(SettleSeconds, "play to settle");

        StartRecording();
        yield return Wait.Seconds(recordSeconds, "the recording");

        Report();
    }

    /// <summary>Called once a frame, from the game's own update.</summary>
    public static void Update()
    {
        if (!Enabled || finished)
        {
            return;
        }

        if (!clock.IsRunning)
        {
            Begin();
            return;
        }

        if (recording && !Focused)
        {
            unfocusedFrames++;
        }

        double waited = clock.Elapsed.TotalSeconds - waitStartedAt;

        if (wait.TimedOut(waited))
        {
            Log($"gave up waiting for {wait.what}");
            Finish();
            return;
        }

        if (wait.IsDone(waited))
        {
            Advance();
        }
    }

    private static readonly Stopwatch clock = new();

    private static IEnumerator<Wait>? script;
    private static Wait wait;
    private static double waitStartedAt;
    private static bool finished;

    private static bool recording;
    private static bool focusedAtStart;
    private static int unfocusedFrames;

    private static bool Focused => CDTXMania.app?.maniaGl?.host?.IsWindowFocused ?? false;

    private static void Begin()
    {
        clock.Start();

        CDTXMania.app?.maniaGl?.host?.FocusWindow();

        script = Script();
        Advance();
    }

    private static void Advance()
    {
        if (script?.MoveNext() == true)
        {
            wait = script.Current;
            waitStartedAt = clock.Elapsed.TotalSeconds;
            Log($"waiting for {wait.what}");
            return;
        }

        Finish();
    }

    private static void StartRecording()
    {
        FrameTrace.Start();

        //so the means below cover the recorded window and not the menus before it
        AllocationProbe.Reset();

        recording = true;
        focusedAtStart = Focused;
        Log($"recording, window focused: {focusedAtStart}");
    }

    private static void Report()
    {
        recording = false;
        Log($"wrote {FrameTrace.Export()} from {FrameTrace.Frames} frames");

        //a run behind another window still produces a trace, and its draw numbers are real, but the input
        //devices were never acquired so nothing about input in it is
        if (!focusedAtStart || unfocusedFrames > 0)
        {
            Log($"WARNING: window did not hold focus for {unfocusedFrames} recorded frames. "
                + "Draw measurements stand; input ones do not.");
        }

        //the window this normally shows in is not open during a scripted run
        for (int probe = 0; probe < AllocationProbe.Count; probe++)
        {
            Log($"alloc {AllocationProbe.NameOf(probe)}: "
                + $"{AllocationProbe.BytesPerFrame(probe)} bytes/frame mean over "
                + $"{AllocationProbe.Frames} frames");
        }
    }

    private static void Finish()
    {
        finished = true;
        script = null;
        CDTXMania.app?.maniaGl?.RequestExit();
    }

    private static void Log(string what)
        => Trace.TraceInformation($"[perftest] {clock.Elapsed.TotalSeconds:0.0}s  {what}");

    /// <summary>What the script is waiting for: a length of time, or a condition it eventually gives up
    /// on.</summary>
    private readonly record struct Wait(string what, Func<bool>? until, double seconds)
    {
        public static Wait Seconds(double seconds, string what) => new(what, null, seconds);

        public static Wait Until(string what, Func<bool> condition) => new(what, condition, GiveUpSeconds);

        public bool IsDone(double waited) => until?.Invoke() ?? waited >= seconds;

        public bool TimedOut(double waited) => until != null && waited >= seconds;
    }
}
