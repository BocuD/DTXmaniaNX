using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.OpenGL
{ 
    public sealed class TextureInspector : IDisposable
    {
        public record TextureEntry(uint Id, int Width, int Height)
        {
            public string? Name { get; init; }
        }

        private readonly OpenGlRenderer _renderer;
        private readonly Dictionary<uint, TextureEntry> _entries = new();

        private string _filter = string.Empty;
        private uint? _selectedId;
        
        public TextureInspector(OpenGlRenderer renderer, IEnumerable<OpenGlRenderer.TextureInfo>? initialSnapshot = null)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            //subscribe to renderer events to update our store
            _renderer.TextureCreated += OnTextureCreatedEvent;
            _renderer.TextureDeleted += OnTextureDeletedEvent;
            _renderer.RendererDisposed += OnRendererDisposedEvent;

            if (initialSnapshot != null)
            {
                foreach (var info in initialSnapshot)
                {
                    _entries[info.Id] = new TextureEntry(info.Id, info.Width, info.Height);
                }
            }
        }

        private void OnTextureCreatedEvent(uint id, int width, int height)
        {
            if (_entries.TryGetValue(id, out var existing))
            {
                _entries[id] = existing with { Width = width, Height = height };
            }
            else
            {
                _entries[id] = new TextureEntry(id, width, height);
            }
        }

        private void OnTextureDeletedEvent(uint id)
        {
            _entries.Remove(id);
            if (_selectedId == id)
                _selectedId = null;
        }

        private void OnRendererDisposedEvent()
        {
            _entries.Clear();
            _selectedId = null;
        }

        public void Dispose()
        {
            //unsubscribe, clean up nicely
            _renderer.TextureCreated -= OnTextureCreatedEvent;
            _renderer.TextureDeleted -= OnTextureDeletedEvent;
            _renderer.RendererDisposed -= OnRendererDisposedEvent;
        }
        
        public void DrawWindow()
        {
            if (!ImGui.Begin("Texture Inspector", ImGuiWindowFlags.NoFocusOnAppearing))
            {
                ImGui.End();
                return;
            }

            ImGui.InputText("Filter", ref _filter, 256);
            ImGui.SameLine();
            if (ImGui.Button("Clear Filter")) _filter = string.Empty;

            ImGui.Separator();

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 250);

            //left: list
            ImGui.BeginChild("TextureList", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()));

            var entriesSnapshot = _entries.Values.ToArray();

            var filtered = string.IsNullOrWhiteSpace(_filter)
                ? entriesSnapshot
                : entriesSnapshot.Where(e => (e.Id.ToString().Contains(_filter) || (e.Name != null && e.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)))).ToArray();

            foreach (var e in filtered.OrderBy(t => t.Id))
            {
                ImGui.PushID((int)e.Id);

                bool isSelected = _selectedId == e.Id;
                if (ImGui.Selectable($"ID: {e.Id}  {e.Width}x{e.Height}", isSelected))
                {
                    _selectedId = e.Id;
                }

                ImGui.SameLine();
                
                ImGui.Dummy(new Vector2(48, 48));

                Vector2 pMin = ImGui.GetItemRectMin();
                Vector2 pMax = ImGui.GetItemRectMax();
                try
                {
                    unsafe
                    {
                        ImTextureRef textureRef = new(null, e.Id);
                        ImGui.GetWindowDrawList().AddImage(textureRef, pMin, pMax);
                    }
                }
                catch
                {
                    // If the backend doesn't support Image for GL ids or fails, ignore thumbnail
                }

                ImGui.PopID();
            }

            ImGui.EndChild();

            ImGui.NextColumn();

            //right: details for selected
            if (_selectedId.HasValue && _entries.TryGetValue(_selectedId.Value, out var selected))
            {
                ImGui.Text($"ID: {selected.Id}");
                ImGui.Text($"Size: {selected.Width} x {selected.Height}");

                ImGui.Separator();

                //preview area
                var avail = ImGui.GetContentRegionAvail();
                var previewSize = new Vector2(Math.Min(avail.X, 512), Math.Min(avail.Y * 0.7f, 512));
                
                ImGui.Dummy(previewSize);

                Vector2 pMin = ImGui.GetItemRectMin();
                Vector2 pMax = ImGui.GetItemRectMax();
                try
                {
                    unsafe
                    {
                        ImTextureRef textureRef = new(null, _selectedId.Value);
                        ImGui.GetWindowDrawList().AddImage(textureRef, pMin, pMax);
                    }
                }
                catch
                {
                    ImGui.TextWrapped("Preview not available for this backend.");
                }

                ImGui.Separator();

                ImGui.SameLine();
                if (ImGui.Button("Copy ID"))
                {
                    ImGui.SetClipboardText(selected.Id.ToString());
                }
            }
            else
            {
                ImGui.Text("No texture selected.");
            }

            ImGui.Columns(1);
            ImGui.End();
        }
    }
}