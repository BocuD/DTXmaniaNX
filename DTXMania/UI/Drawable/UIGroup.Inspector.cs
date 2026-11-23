using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public partial class UIGroup
{
    private void DrawComponentInspector()
    {
        if (!IsComponent)
        {
            return;
        }

        if (!ImGui.CollapsingHeader("Component", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        ImGui.LabelText("Name", componentName);

        string? path = ComponentPath();
        bool inSkin = path != null && File.Exists(path);
        ImGui.LabelText("Source", inSkin ? component : "Default skin");

        //enabled without a file, since saving is what creates one
        ImGui.BeginDisabled(CDTXMania.SkinManager.currentSkin == null);
        if (ImGui.Button("Save Component"))
        {
            WriteIntoSkin();
            ReloadOtherInstances();
        }

        ImGui.EndDisabled();
        ImGui.SameLine();

        ImGui.BeginDisabled(!inSkin);
        if (ImGui.Button("Edit Component"))
        {
            ComponentEditor.Open(component, GetType());
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload Component"))
        {
            ReloadComponent();
        }

        ImGui.EndDisabled();
    }

    //each placement holds the copy it loaded
    private void ReloadOtherInstances()
    {
        if (ComponentPath() is not { } path)
        {
            return;
        }

        UIDrawable top = this;
        while (top.parent != null)
        {
            top = top.parent;
        }

        ReloadInstancesOf(top, path);
    }

    private void ReloadInstancesOf(UIDrawable node, string path)
    {
        if (node is not UIGroup group)
        {
            return;
        }

        if (group != this && group.IsComponent && group.ComponentPath() == path)
        {
            group.ReloadComponent();
            return;
        }

        //a reload replaces the children, so the walk goes over a copy of them
        foreach (UIDrawable child in group.children.ToArray())
        {
            ReloadInstancesOf(child, path);
        }
    }


    private void DrawGroupSection()
    {
        if (!ImGui.CollapsingHeader("Group"))
        {
            return;
        }

        ImGui.Checkbox("Sort by Render Order", ref sortByRenderOrder);
        Inspector.Inspector.Inspect("Tint", ref tint);

        if (ImGui.InputFloat("Perspective", ref perspective, 10.0f, 100.0f, "%.0f"))
        {
            perspective = MathF.Max(perspective, 0.0f);
        }

        ImGui.BeginDisabled(perspective <= 0.0f);
        Inspector.Inspector.Inspect("Vanishing Point", ref vanishingPoint);
        ImGui.EndDisabled();
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        DrawGroupSection();
        DrawComponentInspector();

        if (ImGui.CollapsingHeader("Animator"))
        {
            if (animator == null)
            {
                if (ImGui.Button("Add Animator"))
                {
                    animator = new Animator();
                }
            }
            else
            {
                animator.DrawInspector(this);

                ImGui.Separator();
                if (ImGui.Button("Remove Animator"))
                {
                    animator = null;
                }
            }
        }
    }
}
