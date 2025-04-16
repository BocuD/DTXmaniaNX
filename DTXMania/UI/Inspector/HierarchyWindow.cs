using System.Numerics;
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

        bool selected = Inspector.inspectorTarget == node.id;
        if (selected)
        {
            rootFlags |= ImGuiTreeNodeFlags.Selected;
        }

        string id = node.GetHashCode().ToString();
        string name = string.IsNullOrWhiteSpace(node.name) ? node.GetType().Name : node.name;

        string contextMenuId = id + "ContextMenu";

        if (selected && node.parent != null)
        {
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 80);
            
            float y = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(y - 20);
            if (ImGui.Button("Move Up"))
            {
                int index = node.parent.GetChildIndex(node);
                if (index > 0)
                {
                    node.parent.SetChildIndex(node, index - 1);
                }
            }
            ImGui.SetCursorPosY(y);
        }
        
        if (node.dontSerialize)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
        }

        if (ImGui.TreeNodeEx(id, rootFlags, name))
        {
            if (node.dontSerialize)
            {
                ImGui.PopStyleColor();
            }
            
            HandleNodeDragDrop(node);

            if (selected && node.parent != null)
            {
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 80);
                float y = ImGui.GetCursorPosY();
                ImGui.SetCursorPosY(y - 3);
                if (ImGui.Button("Move Down"))
                {
                    int index = node.parent.GetChildIndex(node);
                    if (index < node.parent.children.Count - 1)
                    {
                        node.parent.SetChildIndex(node, index + 1);
                    }
                }
                ImGui.SetCursorPosY(y);
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node.id;
            }
            
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                //open context menu
                ImGui.OpenPopup(contextMenuId);
            }

            if (group != null)
            {
                if (group.children.Count != 0)
                {
                    /*
                    //get last item rect
                    Vector2 lastItemPos = ImGui.GetItemRectMin();
                    Vector2 lastItemRect = ImGui.GetItemRectMax();
                        
                    //get middle of the item rect
                    float startY = (lastItemPos.Y + lastItemRect.Y) / 2;
                    Vector2 content = ImGui.GetContentRegionAvail();
                    */
                    
                    for (int index = 0; index < group.children.Count; index++)
                    {
                        UIDrawable child = group.children[index];
                        DrawNode(child);
                        
                        /* lastItemPos = ImGui.GetItemRectMin();
                        lastItemRect = ImGui.GetItemRectMax();
                        
                        float endY = (lastItemPos.Y + lastItemRect.Y) / 2;

                        DrawReorderDragDropArea(startY, endY, content.X, group);
                        ImGui.GetWindowDrawList().AddRect(new Vector2(lastItemPos.X, startY), new Vector2(lastItemPos.X + content.X, endY), ImGui.GetColorU32(0xFF0000FF));

                        startY = endY; */
                    }

                    /*
                    if (group.children.Count != 0)
                    {
                        //draw another drag drop area at the end of the group
                        ImGui.Dummy(new Vector2(content.X, 3));
                        float endY = lastItemRect.Y + 3;
                        
                        DrawReorderDragDropArea(startY, endY, content.X, group);
                        ImGui.GetWindowDrawList().AddRect(new Vector2(lastItemPos.X, startY), new Vector2(lastItemPos.X + content.X, endY), ImGui.GetColorU32(0xFF0000FF));
                    }
                    */
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
            if (node.dontSerialize)
            {
                ImGui.PopStyleColor();
            }
            
            if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node.id;
            }
            
            if (selected && node.parent != null)
            {
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 80);
                float y = ImGui.GetCursorPosY();
                ImGui.SetCursorPosY(y - 3);
                if (ImGui.Button("Move Down"))
                {
                    int index = node.parent.GetChildIndex(node);
                    if (index < node.parent.children.Count - 1)
                    {
                        node.parent.SetChildIndex(node, index + 1);
                    }
                }
                ImGui.SetCursorPosY(y);
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
        
        if (ImGui.BeginPopup(contextMenuId))
        {
            DrawNodeContextMenu(node);
        }
    }
    
    /*
    private void DrawReorderDragDropArea(float startY, float endY, float width, UIGroup group)
    {
        //convert from screen to window space
        startY = ImGui.GetWindowPos().Y + startY;
        endY = ImGui.GetWindowPos().Y + endY;
        
        float oldY = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(startY);
        ImGui.Dummy(new Vector2(width, endY - startY));
        
        if (draggingNode)
        {
            if (ImGui.BeginDragDropTarget())
            {
                ImGuiPayloadPtr ptr = ImGui.AcceptDragDropPayload(nameof(UIDrawable));

                //check if delivery
                if (!ptr.IsNull)
                {
                    string droppedId = Inspector.dragDropPayload;
                    UIDrawable? drawable = DrawableTracker.GetDrawable(droppedId);

                    reparentNode = drawable;
                    reparentGroup = group;
                }

                ImGui.EndDragDropTarget();
            }
        }
        
        ImGui.SetCursorPosY(oldY);
    }
    */

    private bool draggingNode = false;
    
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

            ImGui.Text(string.IsNullOrWhiteSpace(node.name) ? node.GetType().ToString() : node.name);
            ImGui.EndDragDropSource();
            
            draggingNode = true;
        }

        if (draggingNode && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            draggingNode = false;
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