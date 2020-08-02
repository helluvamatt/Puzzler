namespace Puzzler.Models
{
	public class Piece
	{
		public Piece(int x, int y, ConnectionType north, ConnectionType south, ConnectionType east, ConnectionType west)
		{
			X = x;
			Y = y;
			NorthConnection = north;
			SouthConnection = south;
			EastConnection = east;
			WestConnection = west;
		}

		public int X { get; }
		public int Y { get; }
		public ConnectionType NorthConnection { get; }
		public ConnectionType SouthConnection { get; }
		public ConnectionType EastConnection { get; }
		public ConnectionType WestConnection { get; }

		public override bool Equals(object obj) => obj is Piece other && other.X == X && other.Y == Y;

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + X;
				hash = hash * 23 + Y;
				return hash;
			}
		}
	}

	public enum ConnectionType { Edge = 0, Tab = 1, Blank = -1 }
}
