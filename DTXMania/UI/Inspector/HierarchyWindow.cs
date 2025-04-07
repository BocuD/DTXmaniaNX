using System.Reflection;
using DTXMania.UI;
using DTXMania.UI.Drawable;
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
        
        string contextMenuId = id + "ContextMenu";

        if (node.dontSerialize)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0, 0, 1));
        }

        if (ImGui.TreeNodeEx(id, rootFlags, name))
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node;
            }
            
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                //open context menu
                ImGui.OpenPopup(contextMenuId);
            }
            
            if (ImGui.BeginPopup(contextMenuId))
            {
                DrawNodeContextMenu(node);
            }

            if (group != null)
            {
                if (group.children.Count != 0)
                {
                    foreach (UIDrawable child in group.children)
                    {
                        DrawNode(child);
                    }
                }
                else
                {
                    //no children
                    var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                    ImGui.TreeNodeEx(id + "NoChildren", flags, "No children");
                }

                //we need to remove the drawable from the group, but since this method is called recursively, we need to make sure we're actually at the parent level again
                if (InspectorManager.toRemove != null)
                {
                    if (group.children.Contains(InspectorManager.toRemove))
                    {
                        group.RemoveChild(InspectorManager.toRemove);
                    }
                }
            }

            ImGui.TreePop();
        }
        else
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node;
            }
            
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                //open context menu
                ImGui.OpenPopup(contextMenuId);
            }
            
            if (ImGui.BeginPopup(contextMenuId))
            {
                DrawNodeContextMenu(node);
            }
        }
        
        if (node.dontSerialize)
        {
            ImGui.PopStyleColor();
        }
    }

    private void DrawNodeContextMenu(UIDrawable node)
    {
        //add child menu
        if (node is UIGroup group)
        {
            if (ImGui.BeginMenu("Add Child"))
            {
                //get all executing assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                    .ToArray();
                
                //get types that inherit from UIDrawable
                Type[] types = assemblies.SelectMany(x => x.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(UIDrawable)) && !t.IsAbstract)
                    .ToArray();
                    
                //filter by types that have constructors with AddChildMenuAttribute
                types = types.Where(t =>
                {
                    ConstructorInfo? constructor = t.GetConstructor(Type.EmptyTypes);
                    if (constructor == null) return false;
                        
                    var attributes = constructor.GetCustomAttributes(typeof(AddChildMenuAttribute), false);
                    return attributes.Length != 0;
                }).ToArray();

                foreach (Type type in types)
                {
                    string typeName = type.Name;
                    if (ImGui.Selectable(typeName))
                    {
                        //create instance of type using the constructor with the AddChildMenuAttribute
                        ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
                        if (constructor == null) continue;
                        var newChild = (UIDrawable)constructor.Invoke(null);
                        group.AddChild(newChild);
                            
                        //close popup
                        ImGui.CloseCurrentPopup();
                    }
                }
                    
                ImGui.EndMenu();
            }
        }
            
        //delete
        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0, 0, 1));
        if (ImGui.Selectable("Delete"))
        {
            InspectorManager.toRemove = node;
        }
        ImGui.PopStyleColor();

        ImGui.EndPopup();
    }
}