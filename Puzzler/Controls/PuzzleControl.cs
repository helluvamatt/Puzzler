using Puzzler.Models;
using Puzzler.Services;
using Puzzler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.Controls
{
	public class PuzzleControl : FrameworkElement
	{
		private const double SnapDistance = 10.0; // "board" pixels where a piece is 50 pixels wide, actual render pixels are dependent on the Zoom

		private readonly Random _Random;
		private readonly List<PieceGroup> _Groups;
		private readonly List<PuzzlePieceControl> _Pieces;

		private Point? _InitialPoint = null;
		private PieceGroup _MovingGroup = null;
		private PuzzlePieceControl _MovingPiece = null;
		private int _MaxX = 0;
		private int _MaxY = 0;
		private int _MaxSizeX = 0;
		private int _MaxSizeY = 0;
		private bool _IsAnimating = false;
		private bool _IsSolved = false;

		public PuzzleControl()
		{
			_Random = new Random();
			_Groups = new List<PieceGroup>();
			_Pieces = new List<PuzzlePieceControl>();
		}

		#region Dependency properties

		#region Image property

		public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(BitmapSource), typeof(PuzzleControl), new PropertyMetadata(null, OnImageChanged));

		public BitmapSource Image
		{
			get => (BitmapSource)GetValue(ImageProperty);
			set => SetValue(ImageProperty, value);
		}

		private static void OnImageChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var pc = (PuzzleControl)owner;
			pc.BindPieces();
		}

		#endregion

		#region Pieces property

		public static readonly DependencyProperty PiecesProperty = DependencyProperty.Register(nameof(Pieces), typeof(ICollection<Piece>), typeof(PuzzleControl), new PropertyMetadata(null, OnPiecesChanged));

		public ICollection<Piece> Pieces
		{
			get => (ICollection<Piece>)GetValue(PiecesProperty);
			set => SetValue(PiecesProperty, value);
		}

		private static void OnPiecesChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var pc = (PuzzleControl)owner;
			pc.BindPieces();
		}

		#endregion

		#region Zoom property

		public const double MinZoom = 0.5;
		public const double MaxZoom = 2.0;

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(PuzzleControl), new PropertyMetadata(0.5, OnZoomChanged, CoerceZoom));

		public double Zoom
		{
			get => (double)GetValue(ZoomProperty);
			set => SetValue(ZoomProperty, value);
		}

		private static void OnZoomChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var pc = (PuzzleControl)owner;
			pc.UpdateBounds();
		}

		private static object CoerceZoom(DependencyObject owner, object value)
		{
			if (value is double z)
			{
				if (z > MaxZoom) z = MaxZoom;
				if (z < MinZoom) z = MinZoom;
				return z;
			}
			return MaxZoom;
		}

		#endregion

		#region PuzzleControl property

		public static readonly DependencyProperty PuzzleControllerProperty = DependencyProperty.Register(nameof(PuzzleController), typeof(IPuzzleController), typeof(PuzzleControl), new PropertyMetadata(null, OnPuzzleControllerChanged));

		public IPuzzleController PuzzleController
		{
			get => (IPuzzleController)GetValue(PuzzleControllerProperty);
			set => SetValue(PuzzleControllerProperty, value);
		}

		private static void OnPuzzleControllerChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var pc = (PuzzleControl)owner;
			if (e.OldValue is IPuzzleController oldController)
			{
				oldController.RandomizePuzzle -= pc.OnRandomizePuzzle;
				oldController.SolvePuzzle -= pc.OnSolvePuzzle;
			}
			if (e.NewValue is IPuzzleController newController)
			{
				newController.RandomizePuzzle += pc.OnRandomizePuzzle;
				newController.SolvePuzzle += pc.OnSolvePuzzle;
			}
		}

		#endregion

		#region MoveCount property

		public static readonly DependencyProperty MoveCountProperty = DependencyProperty.Register(nameof(MoveCount), typeof(int), typeof(PuzzleControl), new PropertyMetadata(0));

		public int MoveCount
		{
			get => (int)GetValue(MoveCountProperty);
			set => SetValue(MoveCountProperty, value);
		}

		#endregion

		#endregion

		#region Control overrides

		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}

		protected override void OnQueryCursor(QueryCursorEventArgs e)
		{
			if (_Pieces.Any(ppc => ppc.HitTest(e.GetPosition(this))))
			{
				e.Cursor = Cursors.Hand;
				e.Handled = true;
			}
			base.OnQueryCursor(e);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (_IsAnimating)
			{
				e.Handled = true;
			}
			else if (e.LeftButton == MouseButtonState.Pressed)
			{
				var pt = e.GetPosition(this);
				_MovingPiece = _Pieces.LastOrDefault(ppc => ppc.HitTest(pt));
				if (_MovingPiece != null)
				{
					_MovingGroup = FindGroup(_MovingPiece);

					if (_MovingGroup != null) BringToTop(_MovingGroup.Pieces);
					else BringToTop(_MovingPiece);

					CaptureMouse();
					_InitialPoint = pt;

					e.Handled = true;
				}
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (_IsAnimating)
			{
				e.Handled = true;
			}
			else if (_MovingPiece != null && e.LeftButton == MouseButtonState.Pressed && _InitialPoint.HasValue)
			{
				var pt = e.GetPosition(this);
				double dX = (pt.X - _InitialPoint.Value.X) / Zoom;
				double dY = (pt.Y - _InitialPoint.Value.Y) / Zoom;

				double canvasWidth = ActualWidth / Zoom;
				double canvasHeight = ActualHeight / Zoom;

				double x, y, originalX, originalY, movingItemWidth, movingItemHeight;

				if (_MovingGroup != null)
				{
					Rect bounds = _MovingGroup.Bounds;
					originalX = x = bounds.X;
					originalY = y = bounds.Y;
					movingItemWidth = bounds.Width;
					movingItemHeight = bounds.Height;
				}
				else
				{
					var pos = _MovingPiece.Position;
					originalX = x = pos.X;
					originalY = y = pos.Y;
					movingItemWidth = PuzzlePieceControl.GridSize;
					movingItemHeight = PuzzlePieceControl.GridSize;
				}

				// Move the coords
				x += dX;
				y += dY;

				// Work within the bounds of the canvas
				if (x < 0) x = 0;
				if (x > canvasWidth - movingItemWidth) x = canvasWidth - movingItemWidth;
				if (y < 0) y = 0;
				if (y > canvasHeight - movingItemHeight) y = canvasHeight - movingItemHeight;

				// How much delta should we add to _InitialPoint:
				double dpX = (x - originalX) * Zoom;
				double dpY = (y - originalY) * Zoom;
				double pX = _InitialPoint.Value.X + dpX;
				double pY = _InitialPoint.Value.Y + dpY;
				_InitialPoint = new Point(pX, pY);

				if (_MovingGroup != null)
				{
					_MovingGroup.SetGroupOrigin(x, y);
				}
				else
				{
					_MovingPiece.Position = new Point(x, y);
				}
				InvalidateVisual();

				e.Handled = true;
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (_IsAnimating)
			{
				e.Handled = true;
			}
			else if (_MovingPiece != null)
			{
				CheckLinks(_MovingPiece, _MovingGroup);

				ReleaseMouseCapture();

				_InitialPoint = null;
				_MovingGroup = null;

				if (!_IsSolved)
				{
					MoveCount++;

					if (CheckSolution())
					{
						_IsSolved = true;
						PuzzleController?.OnPuzzleCompleted(false, MoveCount);
					}
				}

				e.Handled = true;
			}
			base.OnMouseUp(e);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			foreach (var ppc in _Pieces)
			{
				ppc.Render(drawingContext);
			}
		}

		#endregion

		#region Event handlers

		private void OnRandomizePuzzle(object sender, EventArgs e)
		{
			if (Pieces != null && Pieces.Any())
			{
				bool wasSolved = _IsSolved;
				if (wasSolved)
				{
					// Break all groups
					_Groups.Clear();
					_MovingGroup = null;
					MoveCount = 0;
					_IsSolved = false;
				}

				int maxX = Pieces.Max(p => p.X);
				int maxY = Pieces.Max(p => p.Y);

				// Based on maxX and maxY, determine the range of possible random cell locations
				double maxXPos = (maxX + 2) * PuzzlePieceControl.GridSize;
				double maxYPos = (maxY + 2) * PuzzlePieceControl.GridSize;

				var animator = new Animator(TimeSpan.FromMilliseconds(500), InvalidateVisual);
				animator.Completed += (_, __) =>
				{
					_IsAnimating = false;
					PuzzleController?.OnPuzzleRandomized(wasSolved);
				};

				foreach (PuzzlePieceControl ppc in _Pieces.Where(ppc => !_Groups.Any(g => g.Pieces.Contains(ppc))))
				{
					// Random location for the piece
					double x = _Random.NextDouble() * maxXPos;
					double y = _Random.NextDouble() * maxYPos;

					animator.AddAnimation(new PointAnimation(ppc.Position, new Point(x, y), pt => ppc.Position = pt));
				}

				_IsAnimating = true;
				animator.Start();
			}
		}

		private void OnSolvePuzzle(object sender, EventArgs e)
		{
			if (Pieces != null && Pieces.Any() && !_IsSolved)
			{
				var animator = new Animator(TimeSpan.FromMilliseconds(500), InvalidateVisual);
				animator.Completed += (_, __) =>
				{
					_IsAnimating = false;
					_Groups.Clear();
					_Groups.Add(new PieceGroup(_Pieces));
					_IsSolved = true;
					PuzzleController?.OnPuzzleCompleted(true, 0);
				};

				foreach (PuzzlePieceControl ppc in _Pieces)
				{
					// Solved location for the piece
					double x = ppc.X * PuzzlePieceControl.GridSize;
					double y = ppc.Y * PuzzlePieceControl.GridSize;

					animator.AddAnimation(new PointAnimation(ppc.Position, new Point(x, y), pt => ppc.Position = pt));
				}

				_IsAnimating = true;
				animator.Start();
			}
		}

		#endregion

		#region Private members

		private void BindPieces()
		{
			// Break all groups
			_Groups.Clear();
			_MovingGroup = null;
			MoveCount = 0;
			_IsSolved = false;

			// Remove all pieces
			_Pieces.Clear();

			// Add all items in the collection
			if (Pieces != null && Pieces.Any() && Image != null)
			{
				_MaxX = Pieces.Max(p => p.X);
				_MaxY = Pieces.Max(p => p.Y);

				// Based on maxX and maxY, determine size of canvas
				_MaxSizeX = (_MaxX + 3) * PuzzlePieceControl.GridSize;
				_MaxSizeY = (_MaxY + 3) * PuzzlePieceControl.GridSize;

				// Based on maxX and maxY, determine the range of possible random cell locations
				double maxXPos = (_MaxX + 2) * PuzzlePieceControl.GridSize;
				double maxYPos = (_MaxY + 2) * PuzzlePieceControl.GridSize;

				double x, y;

				foreach (var piece in Pieces)
				{
					// Random location for the piece
					x = _Random.NextDouble() * maxXPos;
					y = _Random.NextDouble() * maxYPos;

					var pc = new PuzzlePieceControl(piece, Image)
					{
						Position = new Point(x, y),
					};
					_Pieces.Add(pc);
				}
			}
			else
			{
				_MaxX = 0;
				_MaxY = 0;
				_MaxSizeX = 0;
				_MaxSizeY = 0;
			}

			// Compute and update bounds for all pieces (if present)
			UpdateBounds();
		}

		private void UpdateBounds()
		{
			foreach (var ppc in _Pieces)
			{
				ppc.Zoom = Zoom;
			}
			MinWidth = _MaxSizeX * Zoom;
			MinHeight = _MaxSizeY * Zoom;
			InvalidateVisual();
		}

		private void BringToTop(PuzzlePieceControl el)
		{
			BringToTop(new PuzzlePieceControl[] { el });
		}

		private void BringToTop(IEnumerable<PuzzlePieceControl> els)
		{
			// Recompute all children ZIndex
			var pieces = new List<PuzzlePieceControl>(_Pieces.Except(els));
			pieces.AddRange(els);
			_Pieces.Clear();
			_Pieces.AddRange(pieces);
			InvalidateVisual();
		}

		private PieceGroup FindGroup(PuzzlePieceControl ppc)
		{
			return _Groups.FirstOrDefault(g => g.Pieces.Contains(ppc));
		}

		private PuzzlePieceControl FindPiece(int x, int y)
		{
			return _Pieces.FirstOrDefault(ppc => ppc.X == x && ppc.Y == y);
		}

		private void CheckLinks(PuzzlePieceControl movedPiece, PieceGroup movedGroup)
		{
			var piecesToCheck = new HashSet<PuzzlePieceControl>();
			if (movedGroup != null)
			{
				foreach (var groupItem in movedGroup.Pieces)
				{
					piecesToCheck.Add(groupItem);
				}
			}
			else
			{
				piecesToCheck.Add(movedPiece);
			}

			foreach (var piece in piecesToCheck)
			{
				if (piece.X > 0)
				{
					// Check piece to left (X - 1)
					var neighbor = FindPiece(piece.X - 1, piece.Y);
					if (SnapCheckNeighborX(piece.Position, neighbor.Position, -PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
				}
				if (piece.Y > 0)
				{
					// Check piece above (Y - 1)
					var neighbor = FindPiece(piece.X, piece.Y - 1);
					if (SnapCheckNeighborY(piece.Position, neighbor.Position, -PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
				}
				if (piece.X < _MaxX)
				{
					// Check piece to right (X + 1)
					var neighbor = FindPiece(piece.X + 1, piece.Y);
					if (SnapCheckNeighborX(piece.Position, neighbor.Position, PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
				}
				if (piece.Y < _MaxY)
				{
					// Check piece below (Y + 1)
					var neighbor = FindPiece(piece.X, piece.Y + 1);
					if (SnapCheckNeighborY(piece.Position, neighbor.Position, PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
				}
			}
		}

		private bool SnapCheck(double a, double b) => Math.Abs(a - b) <= SnapDistance;
		private bool SnapCheckNeighborX(Point loc, Point neighbor, double dX) => SnapCheck(loc.X + dX, neighbor.X) && SnapCheck(loc.Y, neighbor.Y);
		private bool SnapCheckNeighborY(Point loc, Point neighbor, double dY) => SnapCheck(loc.X, neighbor.X) && SnapCheck(loc.Y + dY, neighbor.Y);

		private void MergeGroups(PuzzlePieceControl movedPiece, PuzzlePieceControl neighbor)
		{
			var movedGroup = FindGroup(movedPiece);
			var neighborGroup = FindGroup(neighbor);
			if (neighborGroup != null)
			{
				if (movedGroup != null)
				{
					MergeGroups(movedGroup, neighborGroup);
				}
				else
				{
					neighborGroup.AddPiece(movedPiece);
				}
			}
			else
			{
				if (movedGroup != null)
				{
					movedGroup.AddPiece(neighbor);
				}
				else
				{
					var g = new PieceGroup(neighbor);
					g.AddPiece(movedPiece);
					_Groups.Add(g);
				}
			}
			InvalidateVisual();
		}

		private void MergeGroups(PieceGroup group, PieceGroup neighborGroup)
		{
			if (group == neighborGroup) return;

			neighborGroup.MergeWith(group);
			_Groups.Remove(group);
		}

		private bool CheckSolution()
		{
			// There should be a single group...
			if (_Groups.Count != 1) return false;

			// ... with all the pieces
			if (_Pieces.Except(_Groups[0].Pieces).Any()) return false;

			// Solved!
			return true;
		}

		#endregion

		private class PieceGroup
		{
			private readonly SortedSet<PuzzlePieceControl> _Pieces;

			private PuzzlePieceControl _OriginPiece;

			public PieceGroup(PuzzlePieceControl origin)
			{
				_Pieces = new SortedSet<PuzzlePieceControl>();
				AddPiece(origin);
			}

			public PieceGroup(IEnumerable<PuzzlePieceControl> pieces)
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

			public void MergeWith(PieceGroup other)
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
}
