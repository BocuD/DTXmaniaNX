namespace DTXMania.Core.Video.NewPlayer;

public readonly struct DecodedFrameData
{
    public readonly byte[] RgbaData;
    public readonly double TimeSeconds;
    public readonly long FrameNumber;

    public DecodedFrameData(byte[] rgbaData, double timeSeconds, long frameNumber)
    {
        RgbaData = rgbaData;
        TimeSeconds = timeSeconds;
        FrameNumber = frameNumber;
    }
}
