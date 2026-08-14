using System.Numerics;

namespace DTXMania.UI.Drawable;

public enum UiSizeMode
{
    //follows what the element draws: its texture, or its measured text
    Auto,
    //the layout's to state; neither content nor the parent may overwrite it
    Fixed,
    //follows the parent's box, re-read every frame
    Inherit
}

/// <summary>
/// An element's layout box, in logical pixels. Assigning an axis marks it <see cref="UiSizeMode.Fixed"/>,
/// so a layout claims an axis by setting it and leaves the rest following the content.
/// </summary>
public struct UISize : IEquatable<UISize>
{
    private float _x;
    private float _y;

    public UiSizeMode xMode;
    public UiSizeMode yMode;

    public float X
    {
        get => _x;
        set
        {
            _x = value;
            xMode = UiSizeMode.Fixed;
        }
    }

    public float Y
    {
        get => _y;
        set
        {
            _y = value;
            yMode = UiSizeMode.Fixed;
        }
    }

    public bool Inherits => xMode == UiSizeMode.Inherit || yMode == UiSizeMode.Inherit;

    public static UISize Inherited => new() { xMode = UiSizeMode.Inherit, yMode = UiSizeMode.Inherit };

    public static UISize Auto(Vector2 value) => new() { _x = value.X, _y = value.Y };

    /// <summary>Writes only the axes no layout has claimed.</summary>
    public void SetContent(Vector2 content)
    {
        if (xMode == UiSizeMode.Auto)
        {
            _x = content.X;
        }

        if (yMode == UiSizeMode.Auto)
        {
            _y = content.Y;
        }
    }

    public void SetInherited(Vector2 parentSize)
    {
        if (xMode == UiSizeMode.Inherit)
        {
            _x = parentSize.X;
        }

        if (yMode == UiSizeMode.Inherit)
        {
            _y = parentSize.Y;
        }
    }

    public static implicit operator Vector2(UISize size) => new(size._x, size._y);

    public static implicit operator UISize(Vector2 value) => new() { X = value.X, Y = value.Y };

    //only a Fixed axis holds an authored value, which keeps measured text extents out of a written skin
    public bool Equals(UISize other)
    {
        if (xMode != other.xMode || yMode != other.yMode)
        {
            return false;
        }

        return (xMode != UiSizeMode.Fixed || _x.Equals(other._x))
            && (yMode != UiSizeMode.Fixed || _y.Equals(other._y));
    }

    public override bool Equals(object? obj) => obj is UISize other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        xMode,
        yMode,
        xMode == UiSizeMode.Fixed ? _x : 0f,
        yMode == UiSizeMode.Fixed ? _y : 0f);

    public override string ToString() => $"{_x} {xMode}, {_y} {yMode}";
}
