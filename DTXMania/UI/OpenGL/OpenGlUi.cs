namespace DTXMania.UI.OpenGL;

internal static class OpenGlUi
{
    public static OpenGlUiRenderer? Renderer { get; set; }
    public static OpenGlSkiaTextRenderer? SkiaTextRenderer { get; set; }
    public static OpenGlTextureFactory? TextureFactory { get; set; }
}
