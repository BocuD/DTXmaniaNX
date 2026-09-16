namespace DTXMania.Core;

internal static class DataPath
{
    public static string Normalize(string path) =>
        Path.DirectorySeparatorChar == '\\' ? path : path.Replace('\\', Path.DirectorySeparatorChar);
}
