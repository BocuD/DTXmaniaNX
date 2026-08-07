using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.UI.Inspector;

public static class ResourceImporter
{
    private const string PopupId = "Copy Into Skin##resourceImport";

    private sealed record Request(SkinDescriptor Skin, ResourceType Type, string Path, Action<string, bool> OnChosen);

    private static Request? pending;
    private static bool popupOpened;

    public static void Pick(ResourceType type, Dictionary<string, string> filters, Action<string, bool> onChosen)
    {
        string path = NFD.OpenDialog("", filters);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        SkinDescriptor? skin = CDTXMania.SkinManager.currentSkin;

        if (skin == null)
        {
            onChosen(path, false);
            return;
        }

        //already the skin's, so there is nothing to ask about
        if (path.StartsWith(skin.basePath, StringComparison.OrdinalIgnoreCase))
        {
            onChosen(skin.AddResource(type, path), true);
            return;
        }

        pending = new Request(skin, type, path, onChosen);
        popupOpened = false;
    }

    public static void DrawPending()
    {
        if (pending is not { } request)
        {
            return;
        }

        if (!popupOpened)
        {
            popupOpened = true;
            ImGui.OpenPopup(PopupId);
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(460, 0), ImGuiCond.Appearing);

        if (!ImGui.BeginPopupModal(PopupId, ImGuiWindowFlags.NoSavedSettings))
        {
            return;
        }

        ImGui.TextWrapped($"\"{Path.GetFileName(request.Path)}\" is outside the skin.");
        ImGui.TextWrapped($"Copy it into \"{request.Skin.name}\"?");
        ImGui.Spacing();
        ImGui.TextDisabled("Used in place, it draws now but the layout cannot store a reference to it.");
        ImGui.Spacing();

        if (ImGui.Button("Copy Into Skin"))
        {
            Answer(request, request.Skin.AddResource(request.Type, request.Path), true);
        }

        ImGui.SameLine();
        if (ImGui.Button("Use In Place"))
        {
            Answer(request, request.Path, false);
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            Dismiss();
        }

        ImGui.EndPopup();
    }

    private static void Answer(Request request, string value, bool isSkinResource)
    {
        Dismiss();
        request.OnChosen(value, isSkinResource);
    }

    private static void Dismiss()
    {
        pending = null;
        popupOpened = false;
        ImGui.CloseCurrentPopup();
    }
}
