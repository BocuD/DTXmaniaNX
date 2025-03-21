using System.Runtime.InteropServices;
using System.Globalization;
using SharpDX;
using SharpDX.Direct3D9;

namespace FDK;

[StructLayout( LayoutKind.Sequential )]
public struct TransformedColoredTexturedVertex : IEquatable<TransformedColoredTexturedVertex>
{
	public Vector4	Position;
	public int		Color;
	public Vector2	TextureCoordinates;

	public static int SizeInBytes => Marshal.SizeOf( typeof( TransformedColoredTexturedVertex ) );

	public static VertexFormat Format => ( VertexFormat.Texture1 | VertexFormat.Diffuse | VertexFormat.PositionRhw );

	public TransformedColoredTexturedVertex( Vector4 position, int color, Vector2 textureCoordinates )
	{
		this = new TransformedColoredTexturedVertex();
		Position = position;
		Color = color;
		TextureCoordinates = textureCoordinates;
	}

	public static bool operator ==( TransformedColoredTexturedVertex left, TransformedColoredTexturedVertex right )
	{
		return left.Equals( right );
	}
	public static bool operator !=( TransformedColoredTexturedVertex left, TransformedColoredTexturedVertex right )
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
		return Equals( (TransformedColoredTexturedVertex) obj );
	}
	public bool Equals( TransformedColoredTexturedVertex other )
	{
		return ( ( ( Position == other.Position ) && ( Color == other.Color ) ) && ( TextureCoordinates == other.TextureCoordinates ) );
	}
	public override string ToString()
	{
		return string.Format( CultureInfo.CurrentCulture, "{0} ({1}, {2})", new object[] { Position.ToString(), System.Drawing.Color.FromArgb( Color ).ToString(), TextureCoordinates.ToString() } );
	}
}