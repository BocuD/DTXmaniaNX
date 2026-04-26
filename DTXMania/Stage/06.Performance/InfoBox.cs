using System.Numerics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class InfoBox : UIGroup
{
    // CActivity 実装
    public InfoBox()
    {
        var background = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Performance\info_box.png"))));
        size = background.size;
        anchor = new Vector2(1, 0);
        background.name = "Background";
        position = new Vector3(1270, 10, 0);
        name = "InfoBox";
        
        string path = CDTXMania.DTX.strFolderName + CDTXMania.DTX.PREIMAGE;
        BaseTexture txAlbumArt;
        if (!File.Exists(path))
        {
            txAlbumArt = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));
        }
        else
        {
            txAlbumArt = BaseTexture.LoadFromPath(path);
        }
        
        var albumArt = AddChild(new UIImage(txAlbumArt));
        albumArt.size = new Vector2(64, 64);
        albumArt.position = new Vector3(9, 9, 0);
        albumArt.name = "AlbumArt";

        string songName = "song name";
        string artistName = "artist name";
        
        if (string.IsNullOrEmpty(CDTXMania.DTX.TITLE) || (!CDTXMania.bCompactMode && CDTXMania.ConfigIni.b曲名表示をdefのものにする))
            songName = CDTXMania.confirmedSong.title;
        else
            songName = CDTXMania.DTX.TITLE;

        artistName = CDTXMania.DTX.ARTIST;
        
        //add UIText for stage, songname and artist name
        var stageText = AddChild(new UIText(GetStageNumberText(), 15));
        stageText.name = "Stage";
        stageText.position = new Vector3(77, 7, 0);
        stageText.fillColor = new Color4(0.5f, 0.5f, 0.5f);
        stageText.outlineWidth = 0;
        
        var songNameText = AddChild(new UIText(songName, 15));
        songNameText.name = "SongTitle";
        songNameText.position = new Vector3(83, 32, 0);
        songNameText.fillColor = Color4.Black;
        songNameText.outlineColor = Color4.White;
        
        var artistNameText = AddChild(new UIText(artistName, 12));
        artistNameText.name = "ArtistName";
        artistNameText.position = new Vector3(83, 53, 0);
        artistNameText.fillColor = Color4.Black;
        artistNameText.outlineColor = Color4.White;
    }

    private string GetStageNumberText()
    {
        if (CDTXMania.nStageNumber == 1)
            return "1st STAGE";
        if (CDTXMania.nStageNumber == 2)
            return "2nd STAGE";
        if (CDTXMania.nStageNumber == 3)
            return "3rd STAGE";
        if (CDTXMania.nStageNumber > 3)
            return $"{CDTXMania.nStageNumber}th STAGE";
        return "EXTRA STAGE";
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        isVisible = CDTXMania.ConfigIni.bShowMusicInfo;

        base.Draw(parentMatrix);
    }
}