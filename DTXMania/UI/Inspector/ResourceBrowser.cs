using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public static class ResourceBrowser
{
    public static bool Draw(string label, ResourceType type, ref string resource)
    {
        SkinDescriptor? skin = CDTXMania.SkinManager.currentSkin;
        string popupId = $"browse{label}";

        ImGui.BeginDisabled(skin == null);
        if (ImGui.Button($"Browse##{label}"))
        {
            ImGui.OpenPopup(popupId);
        }
        ImGui.EndDisabled();

        if (skin == null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Resources belong to a skin, and none is loaded");
        }

        ImGui.SameLine();
        bool changed = ImGui.InputText(label, ref resource, 512);

        if (skin == null || !ImGui.BeginPopup(popupId))
        {
            return changed;
        }

        string root = Path.Combine(skin.basePath, SkinDescriptor.GetResourceFolder(type));

        if (Directory.Exists(root))
        {
            changed |= DrawFolder(root, root, type, ref resource);
        }
        else
        {
            ImGui.TextDisabled("This skin has no resources of this kind yet");
        }

        ImGui.EndPopup();
        return changed;
    }

    private static bool DrawFolder(string root, string folder, ResourceType type, ref string resource)
    {
        bool changed = false;

        foreach (string directory in Directory.GetDirectories(folder))
        {
            if (!ImGui.BeginMenu(Path.GetFileName(directory)))
            {
                continue;
            }

            changed |= DrawFolder(root, directory, type, ref resource);
            ImGui.EndMenu();
        }

        bool anyFile = false;

        foreach (string file in Directory.GetFiles(folder))
        {
            if (!IsOfType(file, type))
            {
                continue;
            }

            anyFile = true;

            if (ImGui.Selectable(Path.GetFileName(file)))
            {
                //stored relative to the resource root, which is what GetResource resolves against
                resource = Path.GetRelativePath(root, file).Replace('\\', '/');
                changed = true;
            }
        }

        if (!anyFile && Directory.GetDirectories(folder).Length == 0)
        {
            ImGui.TextDisabled("(empty)");
        }

        return changed;
    }

    private static bool IsOfType(string file, ResourceType type)
    {
        string extension = Path.GetExtension(file).ToLowerInvariant();

        return type switch
        {
            ResourceType.Image => extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" or ".gif",
            ResourceType.Video => extension is ".mp4" or ".avi" or ".mkv" or ".webm" or ".mov" or ".wmv"
                or ".mpg" or ".mpeg" or ".flv" or ".m4v",
            ResourceType.Font => extension is ".ttf" or ".otf",
            ResourceType.Animation => extension == ".json",
            _ => true
        };
    }
}
