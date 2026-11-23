using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;
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
    private UIDrawable? duplicateNode;

    private const string RenameId = "HierarchyRename";
    private UIDrawable? renameNode;
    private bool renameOpening;
    
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

            //an open component editor is a tree of its own, so it gets a root here and everything that
            //works off the selection keeps working
            foreach (ComponentEditor editor in ComponentEditor.open)
            {
                ImGui.SeparatorText(editor.componentPath);
                DrawNode(editor.root);
            }

            DrawRename();
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

        if (duplicateNode != null)
        {
            Duplicate(duplicateNode);
            duplicateNode = null;
        }
    }

    private void DrawRename()
    {
        if (renameNode is not { } node)
        {
            return;
        }

        if (renameOpening)
        {
            ImGui.OpenPopup(RenameId);
            renameOpening = false;
        }

        Inspector.DrawRenamePopup(RenameId, node);

        if (!ImGui.IsPopupOpen(RenameId))
        {
            renameNode = null;
        }
    }

    private static void Duplicate(UIDrawable node)
    {
        if (node.parent is not { } parent)
        {
            return;
        }

        UIGroup wrapper = new("Copy");
        wrapper.children.Add(node);
        string json = SkinHierarchySerializer.SerializeToJsonCompact(wrapper);
        wrapper.children.Clear();

        if (SkinHierarchySerializer.DeserializeFromJson(json)?.children.FirstOrDefault() is not { } copy)
        {
            Trace.TraceError($"Could not duplicate {node.name}.");
            return;
        }

        copy.name = UniqueName(parent, node.name);
        parent.AddChild(copy);
        parent.children.Remove(copy);
        parent.children.Insert(parent.children.IndexOf(node) + 1, copy);

        Inspector.inspectorTarget = copy;
    }

    private static string UniqueName(UIGroup parent, string name)
    {
        string candidate = $"{name} Copy";

        for (int suffix = 2; parent.children.Any(c => c.name == candidate); suffix++)
        {
            candidate = $"{name} Copy {suffix}";
        }

        return candidate;
    }

    private void DrawNode(UIDrawable node, bool inComponent = false)
    {
        UIGroup? group = node as UIGroup;

        bool isComponent = group != null && group.IsComponent;

        ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (group == null) rootFlags |= ImGuiTreeNodeFlags.Leaf;

        bool selected = Inspector.inspectorTarget.Is(node);
        if (selected)
        {
            rootFlags |= ImGuiTreeNodeFlags.Selected;
        }

        string id = node.GetHashCode().ToString();
        string name = string.IsNullOrWhiteSpace(node.name) ? node.GetType().Name : node.name;
        if (isComponent)
        {
            name += "   -> " + (string.IsNullOrWhiteSpace(group!.component) ? "(code default)" : group.component);
        }

        string contextMenuId = id + "ContextMenu";

        //component instance (blue) > inside a component, so not part of this layout (dimmed) > dontSerialize (red)
        bool pushColor = true;
        Vector4 color;
        if (isComponent) color = new Vector4(0.45f, 0.7f, 1.0f, 1f);
        else if (inComponent) color = new Vector4(0.6f, 0.6f, 0.6f, 1f);
        else if (node.dontSerialize) color = new Vector4(1f, 0f, 0f, 1f);
        else { pushColor = false; color = default; }

        if (pushColor)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color);
        }

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2.0f, 0.0f));
        ImGui.PushID(id);
        bool visible = node.isVisible;
        if (ImGui.Checkbox("##visible", ref visible))
        {
            node.isVisible = visible;
        }

        ImGui.PopID();
        ImGui.PopStyleVar();
        ImGui.SameLine();

        bool open = ImGui.TreeNodeEx(id, rootFlags, name);

        if (pushColor)
        {
            ImGui.PopStyleColor();
        }

        if (ImGui.IsItemHovered())
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Inspector.inspectorTarget = node;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup(contextMenuId);
            }
        }

        HandleNodeDragDrop(node);

        if (ImGui.BeginPopup(contextMenuId))
        {
            DrawNodeContextMenu(node);
        }

        if (!open)
        {
            return;
        }

        if (group != null)
        {
            if (group.children.Count != 0)
            {
                for (int index = 0; index < group.children.Count; index++)
                {
                    DrawNode(group.children[index], isComponent || inComponent);
                }
            }
            else
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                ImGui.TreeNodeEx(id + "NoChildren", flags, "No children");
            }

            //this method is recursive, so the removal has to wait until the walk is back at the parent
            if (InspectorManager.toRemove.Target is { } drawable
                && group.children.Contains(drawable))
            {
                group.RemoveChild(drawable);
            }
        }

        ImGui.TreePop();
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
            unsafe
            {
                ImGui.SetDragDropPayload(nameof(UIDrawable), (void*)IntPtr.Zero, 0);
                Inspector.dragDropPayload = node;
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
                reparentNode = Inspector.dragDropPayload.Target;
                reparentGroup = group;
                
                ImGui.EndDragDropTarget();
            }
        }
    }

    private void DrawNodeContextMenu(UIDrawable node)
    {
        if (node is UIGroup { component.Length: > 0 } componentNode && ImGui.Selectable("Edit Component"))
        {
            ComponentEditor.Open(componentNode.component, componentNode.GetType());
        }

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
            
        if (ImGui.Selectable("Rename"))
        {
            renameNode = node;
            renameOpening = true;
        }

        if (node.parent != null && ImGui.Selectable("Duplicate"))
        {
            duplicateNode = node;
        }

        //delete
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
        if (ImGui.Selectable("Delete"))
        {
            InspectorManager.toRemove = node;
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
                    entry.DisplayName = ReadableName(type.Name);
                    entry.PathSegments = [];
                }
                else
                {
                    //path like "Shapes/Rectangle" → folders ["Shapes"], display "Rectangle"
                    var segments = attr.Path.Split('/', StringSplitOptions.RemoveEmptyEntries
                                                        | StringSplitOptions.TrimEntries);

                    if (segments.Length == 0)
                    {
                        entry.DisplayName = ReadableName(type.Name);
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

        //sorted once here, so every level of the menu comes out alphabetical without sorting per frame
        list.Sort((first, second) => string.Compare(MenuPath(first), MenuPath(second), StringComparison.OrdinalIgnoreCase));

        cachedCreators = list.ToArray();
        return cachedCreators;
    }

    private static string MenuPath(DrawableCreatorEntry entry)
        => entry.PathSegments.Length == 0 ? entry.DisplayName : $"{string.Join('/', entry.PathSegments)}/{entry.DisplayName}";

    private static string ReadableName(string typeName)
    {
        ReadOnlySpan<char> name = typeName.AsSpan();
        if (name.StartsWith("UI") && name.Length > 2 && char.IsUpper(name[2]))
        {
            name = name[2..];
        }

        StringBuilder readable = new(name.Length + 4);
        for (int index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]) && !char.IsUpper(name[index - 1]))
            {
                readable.Append(' ');
            }

            readable.Append(name[index]);
        }

        return readable.ToString();
    }

    private void DrawAddChildMenu(UIGroup group)
    {
        //the active skin's components, which are runtime data rather than reflected creator types
        DrawAddComponentsMenu(group);

        //depth is how many path segments of the menu have been consumed
        DrawAddChildMenuLevel(group, GetCreators(), depth: 0, parentPath: []);

        ImGui.Separator();

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

    private void DrawAddComponentsMenu(UIGroup group)
    {
        if (!ImGui.BeginMenu("Components"))
        {
            return;
        }

        if (CDTXMania.SkinManager.currentSkin is { } skin)
        {
            foreach (string path in Skin.SkinManager.ComponentPaths(skin))
            {
                if (ImGui.Selectable(Path.GetFileNameWithoutExtension(path)))
                {
                    group.AddChild(new GenericComponent(path));
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        ImGui.Separator();
        if (ImGui.Selectable("Blank"))
        {
            group.AddChild(new GenericComponent());
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndMenu();
    }

    private void DrawAddChildMenuLevel(UIGroup group, DrawableCreatorEntry[] creators, int depth, string[] parentPath)
    {
        //leaves at this level: entries whose PathSegments length equals `depth`
        //folders at this level: entries with more segments; group by the segment at index `depth`
        var leaves = new List<DrawableCreatorEntry>();
        var folders = new List<string>();

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
                if (!folders.Contains(folderName))
                {
                    folders.Add(folderName);
                }
            }
        }

        //folders first, then leaves — same convention as Unity
        foreach (string folderName in folders)
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