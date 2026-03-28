using System.Drawing;
using System.Numerics;
using DTXMania.UI;
using DTXMania.UI.Drawable;

namespace DTXMania.Core.Video;

public class VideoPlayer : UIDrawable
{
    private readonly FFmpegVideoPlayer player;
    private readonly RectangleF clipRect;
    
    public VideoPlayer(FFmpegVideoPlayer videoPlayer)
    {
        player = videoPlayer;
        
        size = new Vector2(videoPlayer.Width, videoPlayer.Height);
        clipRect = new RectangleF(0, 0, videoPlayer.Width, videoPlayer.Height);
    }
    
    public override void Draw(Matrix4x4 parentMatrix)
    {
        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        BaseTexture tex = player.GetUpdatedTexture();
        if (!tex.isValid())
        {
            return;
        }

        tex.tDraw2DMatrix(combinedMatrix, size, clipRect, Color4.White);
    }
}