using System.Drawing;
using System.Numerics;

namespace DTXMania.UI;

public struct Color4
{
    public static Color4 White = new(1f, 1f, 1f, 1f);

    public float Red;
    public float Green;
    public float Blue;
    public float Alpha;

    public Color4(float red, float green, float blue, float alpha = 1f)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public static Color4 FromColor(Color color)
    {
        return new Color4(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f
        );
    }

    public Vector4 ToVector4()
    {
        return new Vector4(Red, Green, Blue, Alpha);
    }
}