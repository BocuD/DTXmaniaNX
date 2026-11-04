using System.Numerics;
using DTXMania.UI.DynamicElements;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public abstract partial class UIDrawable : IDisposable
{
    public string type => GetType().FullName ?? GetType().Name;
    [Themable] public int renderOrder = 0;
    [Themable] public Vector3 position = Vector3.Zero;

    //where in this element's own box its origin sits
    [Themable] public Vector2 pivot = Vector2.Zero;

    //where in the parent's box position is measured from. 0,0 is the parent's top left, 1,1 its bottom
    //right, so an element pinned to the right edge stays there when the parent is resized
    [Themable] public Vector2 parentAnchor = Vector2.Zero;
    [Themable] public UISize size = UISize.Auto(Vector2.One);
    public virtual Vector2 MeasuredSize => size;
    [Themable] public Vector3 scale = Vector3.One;
    [Themable] public Vector3 rotation = Vector3.Zero;
    [Themable] public string name = string.Empty;
    [Themable] public bool isVisible = true;

    //drives arbitrary [Themable] members from data context keys; see UIBinding
    [Themable] [SkinSerialize] public List<UIBinding> bindings = [];

    public bool dontSerialize = false;

    public Matrix4x4 LocalTransformMatrix => localTransformMatrix;

    protected Matrix4x4 localTransformMatrix = Matrix4x4.Identity;

    [JsonIgnore]
    public UIGroup? parent { get; private set; }

    protected UIDrawable()
    {
        DrawableTracker.Register(this);
    }

    //parent space, like position, so it is added to the same translation and this element's own scale
    //and rotation do not apply to it
    private Vector3 ParentAnchorOffset => parent != null
        ? new Vector3(parentAnchor.X * parent.size.X, parentAnchor.Y * parent.size.Y, 0f)
        : Vector3.Zero;

    //an inheriting axis is placed by its margins, so position and parentAnchor only reach the others. The
    //pivot term cancels the one the anchor matrix applies, so the box's start edge lands on the margin
    private Vector3 LocalTranslation
    {
        get
        {
            Vector3 translation = position + ParentAnchorOffset;

            if (size.xMode == UiSizeMode.Inherit)
            {
                translation.X = size.marginLeft + pivot.X * size.X * scale.X;
            }

            if (size.yMode == UiSizeMode.Inherit)
            {
                translation.Y = size.marginTop + pivot.Y * size.Y * scale.Y;
            }

            return translation;
        }
    }

    public void UpdateLocalTransformMatrix()
    {
        size.SetContent(MeasuredSize);

        if (parent != null && size.Inherits)
        {
            size.SetInherited(parent.size);
        }

        Vector3 anchorOffset = new(-pivot.X * size.X, -pivot.Y * size.Y, 0f);
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(LocalTranslation);
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);
        Matrix4x4 anchorMatrix = Matrix4x4.CreateTranslation(anchorOffset * scale);
        localTransformMatrix = scaleMatrix * anchorMatrix * rotationMatrix * translationMatrix;
    }

    public void SetParent(UIGroup? newParent, bool updateGroup = true)
    {
        if (updateGroup)
        {
            parent?.RemoveChild(this);
            newParent?.AddChild(this, false);
        }

        parent = newParent;
    }

    public abstract void Draw(Matrix4x4 parentMatrix);

    public virtual void Dispose()
    {
        DrawableTracker.Remove(this);
    }

    public virtual void OnDeserialize()
    {
        DrawableTracker.Register(this);
    }

    //lets a drawable keep transient runtime state out of a saved layout, e.g. a dynamic UIText's text
    /// <summary>
    /// Whether a member is worth writing into a layout. A member a binding drives is not: its value is
    /// whatever the data said last frame, and the binding is the thing that describes it.
    /// </summary>
    public virtual bool ShouldSerializeMember(string memberName)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].target == memberName)
            {
                return false;
            }
        }

        return true;
    }

    //called by the parent group each frame, before its animator ticks, so animation wins on a shared
    //member. A no-op when nothing is bound, leaving code-set values untouched.
    public void ApplyBindings()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            bindings[i].Apply(this);
        }
    }

    public Matrix4x4 GetFullTransformMatrix()
    {
        Matrix4x4 combined = localTransformMatrix;
        UIGroup? currentParent = parent;
        int iterations = 0;

        while (currentParent != null && iterations < 100)
        {
            combined *= currentParent.localTransformMatrix;
            currentParent = currentParent.parent;
            iterations++;
        }

        return combined;
    }
}
