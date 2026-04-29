namespace DTXMania.CubeTest;

internal static class MatrixUtils
{
    public static float[] CreateRotationX(float radians)
    {
        float[] matrix = new float[16];
        matrix[0] = 1f;
        matrix[5] = MathF.Cos(radians);
        matrix[6] = MathF.Sin(radians);
        matrix[9] = -MathF.Sin(radians);
        matrix[10] = MathF.Cos(radians);
        matrix[15] = 1f;
        return matrix;
    }

    public static void CreateRotationY(Span<float> matrix, float radians)
    {
        matrix.Clear();
        matrix[0] = MathF.Cos(radians);
        matrix[2] = -MathF.Sin(radians);
        matrix[5] = 1f;
        matrix[8] = MathF.Sin(radians);
        matrix[10] = MathF.Cos(radians);
        matrix[15] = 1f;
    }

    public static float[] CreateTranslation(float x, float y, float z)
    {
        float[] matrix = new float[16];
        matrix[0] = 1f;
        matrix[5] = 1f;
        matrix[10] = 1f;
        matrix[12] = x;
        matrix[13] = y;
        matrix[14] = z;
        matrix[15] = 1f;
        return matrix;
    }

    public static float[] CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        float[] matrix = new float[16];
        float f = 1f / MathF.Tan(fieldOfView / 2f);

        matrix[0] = f / aspectRatio;
        matrix[5] = f;
        matrix[10] = (farPlane + nearPlane) / (nearPlane - farPlane);
        matrix[11] = -1f;
        matrix[14] = (2f * farPlane * nearPlane) / (nearPlane - farPlane);
        return matrix;
    }

    public static void Multiply(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
    {
        float[] temp = new float[16];

        for (int column = 0; column < 4; column++)
        {
            for (int row = 0; row < 4; row++)
            {
                temp[column * 4 + row] =
                    left[0 * 4 + row] * right[column * 4 + 0] +
                    left[1 * 4 + row] * right[column * 4 + 1] +
                    left[2 * 4 + row] * right[column * 4 + 2] +
                    left[3 * 4 + row] * right[column * 4 + 3];
            }
        }

        temp.CopyTo(result);
    }
}
