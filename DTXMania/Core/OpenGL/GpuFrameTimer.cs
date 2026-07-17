using Silk.NET.OpenGL;

namespace DTXMania.Core.OpenGL;

/// <summary>
/// Measures whole-frame GPU time with GL_TIME_ELAPSED queries. Results are read back a few
/// frames late and only when available, so the timer never stalls the pipeline. This is the
/// number that tells you whether a stage is GPU-bound (fill rate / overdraw) as opposed to
/// CPU/driver-bound (draw submission), which the FrameProfiler CPU sections can't distinguish.
/// </summary>
internal sealed unsafe class GpuFrameTimer
{
    private const int QueryCount = 4;

    private GL? _gl;
    private readonly uint[] _queries = new uint[QueryCount];
    private readonly bool[] _pending = new bool[QueryCount];
    private int _writeIndex;
    private bool _queryActive;

    public float GpuFrameMs { get; private set; }

    public void AttachGraphics(GL gl)
    {
        //query objects are per-context and die with the old context on window recreation, so
        //always generate a fresh set (the old ids don't need deleting).
        _gl = gl;
        fixed (uint* ids = _queries)
        {
            gl.GenQueries(QueryCount, ids);
        }

        Array.Clear(_pending);
        _writeIndex = 0;
        _queryActive = false;
        GpuFrameMs = 0f;
    }

    public void BeginFrame()
    {
        if (_gl == null || _queryActive)
        {
            return;
        }

        //the slot we're about to reuse must be free; if the GPU is more than QueryCount frames
        //behind, skip this frame's measurement instead of stalling on the result.
        if (_pending[_writeIndex])
        {
            TryHarvestResults();
            if (_pending[_writeIndex])
            {
                return;
            }
        }

        _gl.BeginQuery(GLEnum.TimeElapsed, _queries[_writeIndex]);
        _queryActive = true;
    }

    public void EndFrame()
    {
        if (_gl == null || !_queryActive)
        {
            return;
        }

        _gl.EndQuery(GLEnum.TimeElapsed);
        _pending[_writeIndex] = true;
        _writeIndex = (_writeIndex + 1) % QueryCount;
        _queryActive = false;
        TryHarvestResults();
    }

    private void TryHarvestResults()
    {
        if (_gl == null)
        {
            return;
        }

        //read results oldest-first (the slot at _writeIndex is the next to be reused, i.e. the
        //oldest); stop at the first result the GPU hasn't produced yet.
        for (int i = 0; i < QueryCount; i++)
        {
            int index = (_writeIndex + i) % QueryCount;
            if (!_pending[index])
            {
                continue;
            }

            int available = 0;
            _gl.GetQueryObject(_queries[index], GLEnum.QueryResultAvailable, &available);
            if (available == 0)
            {
                return;
            }

            ulong nanoseconds = 0;
            _gl.GetQueryObject(_queries[index], GLEnum.QueryResult, &nanoseconds);
            GpuFrameMs = nanoseconds / 1_000_000f;
            _pending[index] = false;
        }
    }
}
