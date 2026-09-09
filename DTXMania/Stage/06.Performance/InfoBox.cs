using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;

namespace DTXMania;

internal class InfoBox : UIGroup
{
    public InfoBox() : base("InfoBox")
    {
        size = new Vector2(304, 84);
        pivot = new Vector2(1, 0);

        MakeComponent("InfoBox", InfoBoxDefault);
    }

    private static UIGroup InfoBoxDefault()
    {
        UIGroup root = new("InfoBox");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\Performance\info_box.png")
        });

        root.AddChild(new UIImage
        {
            name = "AlbumArt",
            imageSource = ImageSource.Dynamic,
            dynamicSource = "Song.AlbumArt",
            size = new Vector2(64, 64),
            position = new Vector3(9, 9, 0)
        });

        UIText stage = root.AddChild(new UIText("", 15));
        stage.name = "Stage";
        stage.position = new Vector3(77, 7, 0);
        stage.fillColor = new Color4(0.5f, 0.5f, 0.5f);
        stage.outlineWidth = 0;
        stage.bindings.Add(new UIBinding("text", "Song.StageNumber"));

        Scrolling(root, "SongTitle", "Song.Title", 15, 210.0f, new Vector3(83, 32, 0));
        Scrolling(root, "ArtistName", "Song.Artist", 12, 213.0f, new Vector3(83, 53, 0));

        return root;
    }

    //a title too long for the box travels rather than being cut off
    private static void Scrolling(UIGroup root, string name, string source, int fontSize, float width,
        Vector3 position)
    {
        HorizontallyScrollingText text = root.AddChild(new HorizontallyScrollingText("", fontSize));
        text.name = name;
        text.position = position;
        text.fillColor = Color4.Black;
        text.outlineColor = Color4.White;
        text.size.X = width;
        text.scrollingEnabled = true;
        text.scrollSpeed = 15.0f;
        text.pauseDuration = 5.0f;
        text.bindings.Add(new UIBinding("text", source));
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        isVisible = CDTXMania.ConfigIni.bShowMusicInfo;

        base.Draw(parentMatrix);
    }
}
