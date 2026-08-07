using System.Globalization;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// A mutable <see cref="IUIDataContext"/>. Values reach it three ways:
/// <list type="bullet">
/// <item>pushed strings (<see cref="SetString"/>) — for anything that needs formatting, since formatting
/// on every read would allocate once per bound element per frame,</item>
/// <item>texture providers (<see cref="RegisterTexture"/>) — pulled, because handing back a texture
/// reference costs nothing and pulling avoids ordering bugs when art loads asynchronously,</item>
/// <item>registered objects (<see cref="RegisterObject{T}"/>) — whose <see cref="DataFieldAttribute"/>
/// members become bindable as "Name.Property", read live through the provider.</item>
/// </list>
/// Pushed keys take precedence over object paths.
/// </summary>
public sealed class UIDataContext : IUIDataContext
{
    //app-wide context, consulted after every per-element context; register long-lived objects at startup
    public static readonly UIDataContext Global = new();

    private readonly Dictionary<string, string> strings = new();
    private readonly Dictionary<string, BaseTexture> textures = new();
    private readonly Dictionary<string, Func<BaseTexture?>> textureProviders = new();

    //objects reached by a "Name.path" key. The provider is read live on each resolve so a swapped
    //instance (e.g. the current song) is picked up without a push; the declared type is kept for
    //inspector enumeration, since the instance may be null right now
    private readonly Dictionary<string, Func<object?>> objects = new();
    private readonly Dictionary<string, Type> objectTypes = new();

    //declaring a key (with no value yet) makes it appear in the inspector dropdowns
    public void DeclareString(string key) => strings.TryAdd(key, string.Empty);
    public void DeclareTexture(string key) => textures.TryAdd(key, BaseTexture.None);

    public void SetString(string key, string value) => strings[key] = value;
    public void SetTexture(string key, BaseTexture value) => textures[key] = value;

    //a texture whose current value is fetched on each read, for art that arrives after the fact
    public void RegisterTexture(string key, Func<BaseTexture?> provider) => textureProviders[key] = provider;

    public void RegisterObject<T>(string name, Func<T?> provider) where T : class
    {
        objects[name] = () => provider();
        objectTypes[name] = typeof(T);
    }

    public bool TryGetString(string key, out string value)
    {
        if (strings.TryGetValue(key, out string? stored))
        {
            value = stored;
            return true;
        }

        BindingPath path = DataFieldReflector.ParseKey(key);
        if (TryGetRoot(path, out object? root)
            && DataFieldReflector.TryResolve(root, path.Segments, out object? raw, out DataFieldAttribute? meta))
        {
            value = DataFieldReflector.FormatAsString(raw, path.Format ?? meta?.Format);
            return true;
        }

        value = string.Empty;
        return false;
    }

    public bool TryGetTexture(string key, out BaseTexture texture)
    {
        if (textures.TryGetValue(key, out BaseTexture? stored))
        {
            texture = stored;
            return true;
        }

        if (textureProviders.TryGetValue(key, out Func<BaseTexture?>? provider))
        {
            texture = provider() ?? BaseTexture.None;
            return true;
        }

        BindingPath path = DataFieldReflector.ParseKey(key);
        if (TryGetRoot(path, out object? root)
            && DataFieldReflector.TryResolve(root, path.Segments, out object? raw, out _)
            && raw is BaseTexture resolved)
        {
            texture = resolved;
            return true;
        }

        texture = BaseTexture.None;
        return false;
    }

    public bool TryGetBool(string key, out bool value)
    {
        //a manually-pushed "true"/"false" string can drive a bool binding too
        if (strings.TryGetValue(key, out string? stored) && DataFieldReflector.TryCoerceBool(stored, out value))
        {
            return true;
        }

        BindingPath path = DataFieldReflector.ParseKey(key);
        if (TryGetRoot(path, out object? root) && DataFieldReflector.TryResolveBool(root, path.Segments, out value))
        {
            return true;
        }

        value = false;
        return false;
    }

    public bool TryGetNumber(string key, out double value)
    {
        if (strings.TryGetValue(key, out string? stored) && DataFieldReflector.TryCoerceNumber(stored, out value))
        {
            return true;
        }

        BindingPath path = DataFieldReflector.ParseKey(key);
        if (TryGetRoot(path, out object? root) && DataFieldReflector.TryResolveNumber(root, path.Segments, out value))
        {
            return true;
        }

        value = 0.0;
        return false;
    }

    //the live instance a parsed key starts from; read through the provider so a swapped instance is
    //picked up without the owner having to push anything
    private bool TryGetRoot(BindingPath path, out object? root)
    {
        if (objects.TryGetValue(path.ObjectName, out Func<object?>? provider))
        {
            root = provider();
            return true;
        }

        root = null;
        return false;
    }

    public IEnumerable<string> AvailableKeys(DataBindingKind kind)
    {
        if (kind == DataBindingKind.String)
        {
            foreach (string key in strings.Keys)
            {
                yield return key;
            }
        }
        else if (kind == DataBindingKind.Texture)
        {
            foreach (string key in textures.Keys)
            {
                yield return key;
            }

            foreach (string key in textureProviders.Keys)
            {
                yield return key;
            }
        }

        foreach ((string name, Type type) in objectTypes)
        {
            foreach (DataFieldPath field in DataFieldReflector.EnumeratePaths(type))
            {
                if (Array.IndexOf(KindsFor(field.ValueType), kind) >= 0)
                {
                    yield return $"{name}.{field.Path}";
                }
            }
        }
    }

    //numbers and bools are string-coercible so they appear in string dropdowns too; textures are texture-only
    private static DataBindingKind[] KindsFor(Type type)
    {
        if (typeof(BaseTexture).IsAssignableFrom(type))
        {
            return [DataBindingKind.Texture];
        }

        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual == typeof(bool))
        {
            return [DataBindingKind.Bool, DataBindingKind.String];
        }

        if (IsNumeric(actual))
        {
            return [DataBindingKind.Number, DataBindingKind.String];
        }

        return [DataBindingKind.String];
    }

    private static bool IsNumeric(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}
