using System.Reflection;
using System.Runtime.InteropServices;

namespace DTXMania.Core;

internal static class NativeLibraries
{
    private static readonly string runtimeIdentifier = GetRuntimeIdentifier();

    public static string DirectoryFor(string library) =>
        Path.Combine(AppContext.BaseDirectory, "lib", library, runtimeIdentifier);

    public static string PathFor(string library, string name) => Path.Combine(DirectoryFor(library), FileName(name));

    //imports not found there fall back to the default search
    public static void ResolveFrom(Assembly assembly, string library) =>
        NativeLibrary.SetDllImportResolver(assembly, (name, _, _) => TryLoad(library, name));

    public static IntPtr TryLoad(string library, string name) =>
        NativeLibrary.TryLoad(PathFor(library, name), out IntPtr handle) ? handle : IntPtr.Zero;

    private static string FileName(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            return $"{name}.dll";
        }

        return OperatingSystem.IsMacOS() ? $"lib{name}.dylib" : $"lib{name}.so";
    }

    private static string GetRuntimeIdentifier()
    {
        string os = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : "linux";

        return $"{os}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
    }
}
