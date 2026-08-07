using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;

namespace DTXMania;

public sealed class TitleRoot : StageRoot
{
    public SoundReference gameStart = new(SkinResource.System(@"Sounds\Game start.ogg"));

    public TitleRoot() : base("Title")
    {
        bgm = new SoundReference(SkinResource.System(@"Sounds\Title.ogg"), exclusive: true);
    }
}
