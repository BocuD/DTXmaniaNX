using DTXMania.UI.Drawable;
using FDK;
using SharpDX;

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
    
    public override void Draw(Matrix parentMatrix)
    {
        UpdateLocalTransformMatrix();
        Matrix combinedMatrix = localTransformMatrix * parentMatrix;

        var tex = player.GetUpdatedTexture();

        CTexture.tDraw2DMatrix(CDTXMania.app.Device, tex, combinedMatrix, size, clipRect);
    }
}