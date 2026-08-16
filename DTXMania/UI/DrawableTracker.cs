using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI;

public class DrawableTracker
{
    public static Dictionary<string, WeakReference<UIDrawable>> drawables = new();
    private static int registrationSuppressionDepth;

    public static IDisposable SuppressRegistration()
    {
        registrationSuppressionDepth++;
        return new RegistrationSuppressionScope();
    }

    public static void Register(UIDrawable drawable)
    {
        if (registrationSuppressionDepth > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(drawable.id))
        {
            throw new InvalidOperationException($"Drawable id is missing for {drawable.GetType().FullName} during registration.");
        }

        drawables[drawable.id] = new WeakReference<UIDrawable>(drawable);
    }

    public static void Remove(UIDrawable drawable)
    {
        drawables.Remove(drawable.id);
    }

    public static void DrawWindow()
    {
        if (ImGui.Begin("Drawables", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.Text($"Count: {drawables.Count}");
            ImGui.SameLine();
            if (ImGui.Button("Run GC"))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            ImGui.BeginTable("DrawablesTable", 3);
            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Name");
            ImGui.TableHeadersRow();

            foreach (var drawable in drawables)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(drawable.Key);
                ImGui.TableNextColumn();
                ImGui.Text(drawable.Value.TryGetTarget(out UIDrawable? target) ? target.type : "null");
                ImGui.TableNextColumn();
                ImGui.Text(drawable.Value.TryGetTarget(out UIDrawable? target2)
                    ? (string.IsNullOrEmpty(target2.name) ? target2.GetType().Name : target2.name)
                    : "null");
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    public static UIDrawable? GetDrawable(string guid)
    {
        if (drawables.TryGetValue(guid, out WeakReference<UIDrawable>? weakReference) &&
            weakReference.TryGetTarget(out UIDrawable? drawable))
        {
            return drawable;
        }

        return null;
    }

    private sealed class RegistrationSuppressionScope : IDisposable
    {
        public void Dispose()
        {
            registrationSuppressionDepth = Math.Max(0, registrationSuppressionDepth - 1);
        }
    }
}
