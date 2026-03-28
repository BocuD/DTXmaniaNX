using DTXMania.UI.Drawable;

namespace DTXMania.UI.Text;

public interface IUiTextRenderer
{
    BaseTexture Render(UiTextRenderRequest request);
}
