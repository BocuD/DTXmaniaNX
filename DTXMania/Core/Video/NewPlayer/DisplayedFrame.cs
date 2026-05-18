using DTXMania.UI.Drawable;

namespace DTXMania.Core.Video.NewPlayer;

public readonly struct DisplayedFrame
{
    public readonly BaseTexture Texture;
    public readonly double TimeSeconds;
    public readonly long FrameNumber;
    public readonly long TotalFrames;
    public readonly double TotalDurationSeconds;
    public readonly bool IsValid;

    public DisplayedFrame(BaseTexture texture, double timeSeconds, long frameNumber, long totalFrames, double totalDurationSeconds)
    {
        Texture = texture;
        TimeSeconds = timeSeconds;
        FrameNumber = frameNumber;
        TotalFrames = totalFrames;
        TotalDurationSeconds = totalDurationSeconds;
        IsValid = true;
    }

    public static DisplayedFrame Empty => new DisplayedFrame();
}
