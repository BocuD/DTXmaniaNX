namespace DTXMania.UI.Drawable;

/// <summary>
/// A hand-placeable <see cref="ComponentInstance"/> with no backing behaviour class: it references a
/// component file by path and renders its content. Added via the hierarchy's "Add Child → Components" menu.
///
/// It has no code default, so its content comes entirely from the referenced file, and its children bind
/// against the ambient data-context chain — ancestor contexts plus the global one. Author such a component
/// against global keys, or place it inside a subtree whose context already provides the keys it needs.
/// </summary>
public sealed class GenericComponent : ComponentInstance
{
    public GenericComponent() : base("Component")
    {
    }

    public GenericComponent(string componentPath) : base("Component")
    {
        component = componentPath;
    }

    protected override UIGroup BuildDefault()
        => new(string.IsNullOrWhiteSpace(component) ? "Component" : Path.GetFileNameWithoutExtension(component));
}
