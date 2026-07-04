using DTXMania.UI.Drawable;

namespace DTXMania.UI.Text;

public interface IUiTextRenderer
{
    //Render text synchronously and upload the result to the GPU directly. Returns valid
    //BaseTexture with rendered text.
    BaseTexture Render(UiTextParameters request);

    //Rasterizes text to CPU-side RGBA pixels without touching the GPU. Safe to call off the
    //main thread; the caller uploads the result on the main thread.
    DecodedPixels RenderToPixels(UiTextParameters request);
}
