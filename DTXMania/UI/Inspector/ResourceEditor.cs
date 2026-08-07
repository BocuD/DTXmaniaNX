using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Draws a <see cref="SkinResource"/>: which root it is under, its path, a browse menu of what that
/// root holds, and an import for a file from anywhere else. One call, so every element that references a
/// file is edited the same way.
///
/// The value is passed by value and written back through <paramref name="apply"/> rather than by ref,
/// because importing a file is answered a frame or more later — by which time a ref would be long gone.
/// </summary>
public static class ResourceEditor
{
    /// <param name="onUseInPlace">What to do with a file the user chose to leave where it is. A layout
    /// cannot store a reference to one, so this is only worth passing where drawing it now is still useful
    /// — an image being previewed, say. Left out, choosing "Use In Place" does nothing.</param>
    public static void Draw(string label, ResourceType type, SkinResource value, Action<SkinResource> apply,
        Action<string>? onUseInPlace = null)
    {
        //everything here is named after the label, which an element's own fields may well share — an image
        //has both a content kind and a file. Scoping the ids keeps those from colliding
        ImGui.PushID(label);

        ResourceSource source = value.source;
        if (Inspector.Inspect($"{label} Location", ref source))
        {
            apply(new SkinResource(source, value.path));
        }

        string path = value.path;
        if (ResourceBrowser.Draw(label, type, RootFor(value.source, type), ref path))
        {
            apply(new SkinResource(value.source, path));
        }

        ImGui.SameLine();
        if (ImGui.Button("Import"))
        {
            //the import decides the source too: a copy belongs to the skin, and a file left where it is
            //cannot be referenced by a layout at all
            ResourceImporter.Pick(type, FiltersFor(type), (imported, isSkinResource) =>
            {
                if (isSkinResource)
                {
                    apply(new SkinResource(ResourceSource.Skin, imported));
                }
                else
                {
                    onUseInPlace?.Invoke(imported);
                }
            });
        }

        if (!value.IsEmpty && !value.Exists(type))
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.6f, 0.4f, 1f), $"{value} does not exist");
        }

        ImGui.PopID();
    }

    //where a source's files live, which is what the browse menu lists
    private static string RootFor(ResourceSource source, ResourceType type)
    {
        if (source == ResourceSource.System)
        {
            //system fonts come from the Fonts folders rather than the built-in skin; see SkinResource
            return type == ResourceType.Font ? UIFonts.SystemFontFolder : SkinManager.SystemRoot;
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
        ResourceType.Sound => new() { { "Sounds", "ogg,wav,mp3,flac,xa" } },
        _ => new Dictionary<string, string>()
    };
}
