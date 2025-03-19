using System.Runtime.InteropServices;
using System.Globalization;
using SharpDX;
using SharpDX.Direct3D9;

namespace FDK;

[StructLayout( LayoutKind.Sequential )]
public struct PositionColoredTexturedVertex : IEquatable<PositionColoredTexturedVertex>
{
	public Vector3	Position;
	public int		Color;
	public Vector2	TextureCoordinates;

	public static int SizeInBytes => Marshal.SizeOf( typeof( PositionColoredTexturedVertex ) );

	public static VertexFormat Format => ( VertexFormat.Texture1 | VertexFormat.Diffuse | VertexFormat.Position );

	public PositionColoredTexturedVertex( Vector3 position, int color, Vector2 textureCoordinates )
	{
		this = new PositionColoredTexturedVertex();
		Position = position;
		Color = color;
		TextureCoordinates = textureCoordinates;
	}

	public static bool operator ==( PositionColoredTexturedVertex left, PositionColoredTexturedVertex right )
	{
		return left.Equals( right );
	}
	public static bool operator !=( PositionColoredTexturedVertex left, PositionColoredTexturedVertex right )
	{
		return !( left == right );
	}
	public override int GetHashCode()
	{
		return ( ( Position.GetHashCode() + Color.GetHashCode() ) + TextureCoordinates.GetHashCode() );
	}
	public override bool Equals( object obj )
	{
		if( obj == null )
		{
			return false;
		}
		if( GetType() != obj.GetType() )
		{
			return false;
		}
		return Equals( (PositionColoredTexturedVertex) obj );
	}
	public bool Equals( PositionColoredTexturedVertex other )
	{
		return ( ( ( Position == other.Position ) && ( Color == other.Color ) ) && ( TextureCoordinates == other.TextureCoordinates ) );
	}
	public override string ToString()
	{
		return string.Format( CultureInfo.CurrentCulture, "{0} ({1}, {2})", new object[] { Position.ToString(), System.Drawing.Color.FromArgb( Color ).ToString(), TextureCoordinates.ToString() } );
	}
}