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

    /// <summary>Where the caret sits after this many characters of the request's first line, in the
    /// pixels <see cref="RenderToPixels"/> produces. It lives here because only the renderer knows where
    /// its own text starts inside the bitmap.</summary>
    float CaretOffset(UiTextParameters request, int characters);

    /// <summary>The caret nearest an offset in those same pixels, which is the one a click asks for.</summary>
    int CaretIndexAt(UiTextParameters request, float offset);
}
