using Hexa.NET.ImGui;

namespace DTXUIRenderer;

public class HierarchyWindow
{
    public UIGroup? target;
    
    public void Draw()
    {
        try
        {
            ImGui.Begin("Hierarchy");

            if (target != null)
            {
                DrawNode(target);
            }
            else
            {
                ImGui.Text("No group selected");
            }
        }
        finally
        {
            ImGui.End();
        }
    }
    
    private void DrawNode(UIGroup node)
    {
        ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        if (node == Inspector.inspectorTarget)
        {
            rootFlags |= ImGuiTreeNodeFlags.Selected;
        }

        if (ImGui.TreeNodeEx(node.name, rootFlags))
        {
            if (ImGui.IsItemClicked())
            {
                Inspector.inspectorTarget = node;
            }

            for (int index = 0; index < node.children.Count; index++)
            {
                UIDrawable child = node.children[index];

                if (child is UIGroup group)
                {
                    DrawNode(group);
                }
                else
                {
                    //leaf node
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                    if (child == Inspector.inspectorTarget)
                    {
                        flags |= ImGuiTreeNodeFlags.Selected;
                    }

                    string childType = child.GetType().Name;
                    ImGui.TreeNodeEx(child.GetHashCode().ToString(), flags, $"{index} - {childType}");

                    if (ImGui.IsItemClicked())
                    {
                        Inspector.inspectorTarget = child;
                    }
                }
            }

            ImGui.TreePop();
        }
        else
        {
            if (ImGui.IsItemClicked())
            {
                Inspector.inspectorTarget = node;
            }
        }
    }
}