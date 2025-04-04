using System;
using System.Linq;
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

                string addChildPopupId = group.GetHashCode().ToString();
                if (ImGui.Button("Add New Child"))
                {
                    //popup for child type
                    ImGui.OpenPopup(addChildPopupId);
                }

                if (ImGui.BeginPopup(addChildPopupId))
                {
                    ImGui.Text("Select child type");
                    ImGui.Separator();
                    
                    //get types that inherit from UIDrawable
                    Type[] types = typeof(UIDrawable).Assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(UIDrawable)) && !t.IsAbstract)
                        .ToArray();

                    foreach (Type type in types)
                    {
                        string typeName = type.Name;
                        if (ImGui.Selectable(typeName))
                        {
                            //create new instance of type
                            UIDrawable newChild = (UIDrawable)Activator.CreateInstance(type)!;
                            group.AddChild(newChild);
                            
                            //close popup
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    
                    ImGui.EndPopup();
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