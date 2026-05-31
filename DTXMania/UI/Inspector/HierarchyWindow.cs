using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

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
            ImGui.Begin("Hierarchy", ImGuiWindowFlags.NoFocusOnAppearing);
            
            DrawNode(CDTXMania.persistentUIGroup);
            
            ImGui.Separator();
            
            target = CDTXMania.StageManager.rCurrentStage.ui;
            
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

            if (ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    Inspector.inspectorTarget = node.id;
                }

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                {
                    //open context menu
                    ImGui.OpenPopup(contextMenuId);
                }
            }
            
            HandleNodeDragDrop(node);
            
            if (ImGui.BeginPopup(contextMenuId))
            {
                DrawNodeContextMenu(node);
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
            if (node.dontSerialize)
            {
                ImGui.PopStyleColor();
            }

            if (ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    Inspector.inspectorTarget = node.id;
                }

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                {
                    //open context menu
                    ImGui.OpenPopup(contextMenuId);
                }
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
            
            if (ImGui.BeginPopup(contextMenuId))
            {
                DrawNodeContextMenu(node);
            }
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
                DrawAddChildMenu(group);
                ImGui.EndMenu();
            }
            
            //serialize
            if (ImGui.Selectable("Serialize Group"))
            {
                //mydocuments
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string path = NFD.SaveDialog(defaultPath, $"{group.name}.json");
                if (!string.IsNullOrEmpty(path))
                {
                    //serialize group to json
                    string json = SkinHierarchySerializer.SerializeToJson(group);
                    File.WriteAllText(path, json);
                }
            }
        }
            
        //delete
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
        if (ImGui.Selectable("Delete"))
        {
            InspectorManager.toRemove = node.id;
        }
        ImGui.PopStyleColor();

        ImGui.EndPopup();
    }

    private class DrawableCreatorEntry
    {
        public string DisplayName = "";       // leaf name shown in the menu
        public string[] PathSegments = [];    // folder segments, empty = root
        public MethodInfo Method = null!;
    }

    private static DrawableCreatorEntry[]? cachedCreators;
    
    private static DrawableCreatorEntry[] GetCreators()
    {
        if (cachedCreators != null) return cachedCreators;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .ToArray();

        var types = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(UIDrawable)) && !t.IsAbstract)
            .ToArray();

        var list = new List<DrawableCreatorEntry>();

        foreach (Type type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(AddChildMenuAttribute), false).Length != 0)
                .ToArray();

            foreach (MethodInfo method in methods)
            {
                var attr = (AddChildMenuAttribute)method.GetCustomAttributes(typeof(AddChildMenuAttribute), false)[0];

                var entry = new DrawableCreatorEntry { Method = method };

                if (string.IsNullOrWhiteSpace(attr.Path))
                {
                    //no path: root-level item named after the type (current behaviour)
                    entry.DisplayName = type.Name;
                    entry.PathSegments = [];
                }
                else
                {
                    //path like "Shapes/Rectangle" → folders ["Shapes"], display "Rectangle"
                    var segments = attr.Path.Split('/', StringSplitOptions.RemoveEmptyEntries
                                                        | StringSplitOptions.TrimEntries);

                    if (segments.Length == 0)
                    {
                        entry.DisplayName = type.Name;
                        entry.PathSegments = [];
                    }
                    else
                    {
                        entry.DisplayName = segments[^1];
                        entry.PathSegments = segments[..^1];
                    }
                }

                list.Add(entry);
            }
        }

        cachedCreators = list.ToArray();
        return cachedCreators;
    }

    private void DrawAddChildMenu(UIGroup group)
    {
        var creators = GetCreators();

        //draw recursively. `depth` is how many path segments we've already consumed.
        DrawAddChildMenuLevel(group, creators, depth: 0, parentPath: []);

        if (ImGui.Selectable("Load from JSON"))
        {
            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = NFD.OpenDialog(defaultPath);

            if (!string.IsNullOrEmpty(path))
            {
                string json = File.ReadAllText(path);
                UIGroup? loadedGroup = SkinHierarchySerializer.DeserializeFromJson(json);
                if (loadedGroup != null)
                {
                    //add loaded group as child
                    group.AddChild(loadedGroup);
                }
                else
                {
                    Trace.TraceError("Failed to load group from JSON");
                }
            }
        }
    }

    private void DrawAddChildMenuLevel(UIGroup group, DrawableCreatorEntry[] creators, int depth, string[] parentPath)
    {
        //leaves at this level: entries whose PathSegments length equals `depth`
        //folders at this level: entries with more segments; group by the segment at index `depth`
        var leaves = new List<DrawableCreatorEntry>();
        var folders = new Dictionary<string, List<DrawableCreatorEntry>>();

        foreach (var entry in creators)
        {
            //must be within the same parent path
            if (entry.PathSegments.Length < depth) continue;

            bool matches = true;
            for (int i = 0; i < depth; i++)
            {
                if (entry.PathSegments[i] != parentPath[i]) { matches = false; break; }
            }
            if (!matches) continue;

            if (entry.PathSegments.Length == depth)
            {
                leaves.Add(entry);
            }
            else
            {
                string folderName = entry.PathSegments[depth];
                if (!folders.TryGetValue(folderName, out var bucket))
                {
                    bucket = new List<DrawableCreatorEntry>();
                    folders[folderName] = bucket;
                }
                bucket.Add(entry);
            }
        }

        //folders first, then leaves — same convention as Unity
        foreach (var (folderName, _) in folders)
        {
            if (ImGui.BeginMenu(folderName))
            {
                var childPath = new string[depth + 1];
                Array.Copy(parentPath, childPath, depth);
                childPath[depth] = folderName;

                DrawAddChildMenuLevel(group, creators, depth + 1, childPath);
                ImGui.EndMenu();
            }
        }

        foreach (var entry in leaves)
        {
            if (ImGui.Selectable(entry.DisplayName))
            {
                object? newChild = entry.Method.Invoke(null, null);
                if (newChild is UIDrawable drawable)
                {
                    group.AddChild(drawable);
                }
                ImGui.CloseCurrentPopup();
            }
        }
    }
}