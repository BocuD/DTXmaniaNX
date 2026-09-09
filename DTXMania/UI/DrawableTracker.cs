using System.Runtime.CompilerServices;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI;

/// <summary>
/// Every drawable that has been constructed, weakly. Nothing resolves a drawable through this; it exists
/// so the Drawables window can list what is still alive.
/// </summary>
public class DrawableTracker
{
    private static readonly List<WeakReference<UIDrawable>> drawables = [];
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

        drawables.Add(new WeakReference<UIDrawable>(drawable));
    }

    public static void Remove(UIDrawable drawable)
    {
        for (int i = drawables.Count - 1; i >= 0; i--)
        {
            if (!drawables[i].TryGetTarget(out UIDrawable? target) || ReferenceEquals(target, drawable))
            {
                drawables.RemoveAt(i);
            }
        }
    }

    /// <summary>Every live drawable of the given type.</summary>
    public static IEnumerable<T> AllOfType<T>() where T : UIDrawable
    {
        foreach (WeakReference<UIDrawable> reference in drawables)
        {
            if (reference.TryGetTarget(out UIDrawable? target) && target is T match)
            {
                yield return match;
            }
        }
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
                drawables.RemoveAll(reference => !reference.TryGetTarget(out _));
            }

            ImGui.BeginTable("DrawablesTable", 2);
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Name");
            ImGui.TableHeadersRow();

            foreach (WeakReference<UIDrawable> reference in drawables)
            {
                if (!reference.TryGetTarget(out UIDrawable? target))
                {
                    continue;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(target.type);
                ImGui.TableNextColumn();
                ImGui.Text(string.IsNullOrEmpty(target.name) ? target.GetType().Name : target.name);
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private sealed class RegistrationSuppressionScope : IDisposable
    {
        public void Dispose()
        {
            registrationSuppressionDepth = Math.Max(0, registrationSuppressionDepth - 1);
        }
    }
}
