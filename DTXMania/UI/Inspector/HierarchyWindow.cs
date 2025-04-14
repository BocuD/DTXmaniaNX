using System.Reflection;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class HierarchyWindow
{
    public UIDrawable? target;

    private UIDrawable? reparentNode;
    private UIGroup? reparentGroup;
    
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
        
        if (reparentNode != null && reparentGroup != null)
        {
            reparentNode.SetParent(reparentGroup);
            reparentNode = null;
            reparentGroup = null;
        }
    }
    
    private void DrawNode(UIDrawable node)
    {
        UIGroup? group = node as UIGroup;
        
        ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (group == null) rootFlags |= ImGuiTreeNodeFlags.Leaf;
        
        if (node.id == Inspector.inspectorTarget)
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
            HandleNodeDragDrop(node);

            if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node.id;
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
                if (!string.IsNullOrEmpty(InspectorManager.toRemove))
                {
                    var drawable = InspectorManager.toRemoveDrawable;
                    if (drawable == null) return;
                    
                    if (group.children.Contains(drawable))
                    {
                        group.RemoveChild(drawable);
                    }
                }
            }

            ImGui.TreePop();
        }
        else
        {
            if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node.id;
            }
            
            HandleNodeDragDrop(node);
            
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

    private void HandleNodeDragDrop(UIDrawable node)
    {
        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
        {
            Type type = node.GetType();

            unsafe
            {
                ImGui.SetDragDropPayload(nameof(UIDrawable), (void*)IntPtr.Zero, 0);
                Inspector.dragDropPayload = node.id;
                Inspector.dragDropType = type;
            }

            ImGui.Text(node.name);
            ImGui.EndDragDropSource();
        }
            
        //drag and drop target
        if (node is UIGroup group && ImGui.BeginDragDropTarget())
        {
            ImGuiPayloadPtr ptr = ImGui.AcceptDragDropPayload(nameof(UIDrawable));
            
            //check if delivery
            if (ptr.IsNull)
            {
                ImGui.EndDragDropTarget();
            }
            else
            {
                string droppedId = Inspector.dragDropPayload;
                var drawable = DrawableTracker.GetDrawable(droppedId);
                
                reparentNode = drawable;
                reparentGroup = group;
                
                ImGui.EndDragDropTarget();
            }
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
                
                Dictionary<Type, MethodInfo> creators = new();
                
                foreach (Type type in types)
                {
                    //get static methods with AddChildMenuAttribute
                    var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Where(m => m.GetCustomAttributes(typeof(AddChildMenuAttribute), false).Length != 0)
                        .ToArray();

                    if (staticMethods.Length > 0)
                        creators[type] = staticMethods.FirstOrDefault()!;
                }

                foreach (var creator in creators)
                {
                    string typeName = creator.Key.Name;
                    if (ImGui.Selectable(typeName))
                    {
                        //create instance of type using the static method
                        object? newChild = creator.Value.Invoke(null, null);

                        if (newChild is UIDrawable drawable)
                        {
                            group.AddChild(drawable);
                        }
                        
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
            InspectorManager.toRemove = node.id;
        }
        ImGui.PopStyleColor();

        ImGui.EndPopup();
    }
}