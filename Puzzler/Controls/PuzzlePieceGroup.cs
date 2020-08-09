using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Puzzler.Controls
{
	public class PuzzlePieceGroup
	{
		private readonly SortedSet<PuzzlePieceControl> _Pieces;

		private PuzzlePieceControl _OriginPiece;

		public PuzzlePieceGroup(PuzzlePieceControl origin)
		{
			_Pieces = new SortedSet<PuzzlePieceControl>();
			AddPiece(origin);
		}

		public PuzzlePieceGroup(IEnumerable<PuzzlePieceControl> pieces)
		{
			_Pieces = new SortedSet<PuzzlePieceControl>(pieces);
			ComputeBounds();
		}

		public Rect Bounds { get; private set; }

		public IEnumerable<PuzzlePieceControl> Pieces => _Pieces;
		public int Count => _Pieces.Count;

		public void AddPiece(PuzzlePieceControl ppc)
		{
			AddPieceCore(ppc);
			ComputeBounds();
		}

		public void MergeWith(PuzzlePieceGroup other)
		{
			foreach (var ppc in other.Pieces)
			{
				AddPieceCore(ppc);
			}
			ComputeBounds();
		}

		public void SetGroupOrigin(double newOriginX, double newOriginY)
		{
			double oldOriginX = Bounds.X;
			double oldOriginY = Bounds.Y;
			double pieceX, pieceY;
			foreach (var groupItem in Pieces)
			{
				pieceX = groupItem.Position.X - oldOriginX + newOriginX;
				pieceY = groupItem.Position.Y - oldOriginY + newOriginY;
				groupItem.Position = new Point(pieceX, pieceY);
			}
			ComputeBounds();
		}

		private void AddPieceCore(PuzzlePieceControl ppc)
		{
			// Compute the position of the item
			// If this is the first piece in the group, skip this so that item becomes the "origin" and its position is the position of the group
			if (_OriginPiece != null)
			{
				int offsetX = ppc.X - _OriginPiece.X;
				int offsetY = ppc.Y - _OriginPiece.Y;
				double x = _OriginPiece.Position.X + offsetX * PuzzlePieceControl.GridSize;
				double y = _OriginPiece.Position.Y + offsetY * PuzzlePieceControl.GridSize;
				ppc.Position = new Point(x, y);
			}

			// Add the piece to the collection
			_Pieces.Add(ppc);
		}

		private void ComputeBounds()
		{
			_OriginPiece = Pieces.Min();
			Bounds = Pieces.Aggregate(Rect.Empty, (acc, ppc) => Rect.Union(acc, new Rect(ppc.Position.X, ppc.Position.Y, PuzzlePieceControl.GridSize, PuzzlePieceControl.GridSize)));
		}
	}
}
