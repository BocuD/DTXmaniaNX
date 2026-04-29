using System.Numerics;

namespace DTXUIRenderer;

public static class MatrixTools
{
    public static Vector2 TransformPoint(this Matrix4x4 matrix, Vector2 point)
    {
        Vector3 transformed = Vector3.Transform(new Vector3(point, 0f), matrix);
        return new Vector2(transformed.X, transformed.Y);
    }
}
