using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Draws a <see cref="SkinResourceRef"/>: which root it is under, its path, a browse menu of what that
/// root holds, and an import for a file from anywhere else. One call, so every element that references a
/// file is edited the same way.
///
/// The value is passed by value and written back through <paramref name="apply"/> rather than by ref,
/// because importing a file is answered a frame or more later — by which time a ref would be long gone.
/// </summary>
public static class ResourceRefEditor
{
    public static void Draw(string label, ResourceType type, SkinResourceRef value, Action<SkinResourceRef> apply)
    {
        ResourceSource source = value.source;
        if (Inspector.Inspect($"{label} Source", ref source))
        {
            apply(new SkinResourceRef(source, value.path));
        }

        string path = value.path;
        if (ResourceBrowser.Draw(label, type, RootFor(value.source, type), ref path))
        {
            apply(new SkinResourceRef(value.source, path));
        }

        ImGui.SameLine();
        if (ImGui.Button($"Import##{label}"))
        {
            //the import decides the source too: a copy belongs to the skin, and a file left where it is
            //cannot be referenced by a layout at all
            ResourceImporter.Pick(type, FiltersFor(type), (imported, isSkinResource) =>
            {
                if (isSkinResource)
                {
                    apply(new SkinResourceRef(ResourceSource.Skin, imported));
                }
            });
        }

        if (!value.IsEmpty && !value.Exists(type))
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.6f, 0.4f, 1f), $"{value} does not exist");
        }
    }

    //where a source's files live, which is what the browse menu lists
    private static string RootFor(ResourceSource source, ResourceType type)
    {
        if (source == ResourceSource.System)
        {
            return SkinManager.SystemRoot;
        }

        return CDTXMania.SkinManager.currentSkin is { } skin
            ? Path.Combine(skin.basePath, SkinDescriptor.GetResourceFolder(type))
            : string.Empty;
    }

    private static Dictionary<string, string> FiltersFor(ResourceType type) => type switch
    {
        ResourceType.Image => new() { { "Images", "png,jpg,jpeg,bmp,tga,gif" } },
        ResourceType.Video => new() { { "Videos", "mp4,avi,mkv,webm,mov,wmv,mpg,mpeg,flv,m4v" } },
        ResourceType.Font => new() { { "Fonts", "ttf,otf" } },
        ResourceType.Animation => new() { { "Animations", "json" } },
        _ => new Dictionary<string, string>()
    };
}
