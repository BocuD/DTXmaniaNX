namespace DTXMania.Core.Framework;

public abstract class IRenderer
{
    public abstract string name { get; }
    public abstract int lastFrameDrawCalls { get; }
}