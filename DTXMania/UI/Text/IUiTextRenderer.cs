using DTXMania.UI.Drawable;

namespace DTXMania.UI.Text;

public interface IUiTextRenderer
{
    BaseTexture Render(UiTextParameters request);
    //main thread; the caller uploads the result on the main thread.
    DecodedPixels RenderToPixels(UiTextParameters request);
}
