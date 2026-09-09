using DTXMania.UI.Drawable;
using DTXMania.Core.Audio;
using DTXMania.UI;
using DTXMania.UI.Skin;

namespace DTXMania;

public sealed class TitleRoot : StageRoot
{
    public SoundReference gameStart = new(SkinResource.System(@"Sounds\Game start.ogg"))
    {
        finishAfterStage = true
    };

    public TitleRoot() : base("Title")
    {
        bgm = new SoundReference(SkinResource.System(@"Sounds\Title.ogg"), exclusive: true,
            group: AudioGroup.Bgm);

        canvasFit = UiCanvasFit.Fill;
    }
}
