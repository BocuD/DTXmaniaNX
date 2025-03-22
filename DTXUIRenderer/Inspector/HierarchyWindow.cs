using Hexa.NET.ImGui;

namespace DTXUIRenderer;

public class HierarchyWindow
{
    public UIDrawable? target;
    
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
    
    private void DrawNode(UIDrawable node)
    {
        UIGroup? group = node as UIGroup;
        
        ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (group == null) rootFlags |= ImGuiTreeNodeFlags.Leaf;
        
        if (node == Inspector.inspectorTarget)
        {
            rootFlags |= ImGuiTreeNodeFlags.Selected;
        }
        
        string id = node.GetHashCode().ToString();
        string name = string.IsNullOrWhiteSpace(node.name) ? node.GetType().Name : node.name;
        
        if (ImGui.TreeNodeEx(id, rootFlags, name))
        {
            if (ImGui.IsItemClicked())
            {
                Inspector.inspectorTarget = node;
            }

            if (group != null)
            {
                foreach (UIDrawable child in group.children)
                {
                    DrawNode(child);
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