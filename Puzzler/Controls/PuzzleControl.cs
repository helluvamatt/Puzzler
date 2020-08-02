using Puzzler.Models;
using Puzzler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Puzzler.Controls
{
	public class PuzzleControl : Border
	{
		private const double SnapDistance = 10.0; // "board" pixels where a piece is 50 pixels wide, actual render pixels are dependent on the Zoom

		private readonly ScaleTransform _ZoomTransform;
		private readonly Canvas _Canvas;
		private readonly Random _Random;
		private readonly List<PieceGroup> _Groups;

		private Point? _InitialPoint = null;
		private PieceGroup _MovingGroup = null;
		private int _MaxX = 0;
		private int _MaxY = 0;
		private int _MaxSizeX = 0;
		private int _MaxSizeY = 0;
		private bool _IsAnimating = false;
		private bool _IsSolved = false;

		public PuzzleControl()
		{
			_ZoomTransform = new ScaleTransform(1, 1);

			double canvasPadding = (PuzzlePieceControl.TabSize + 3) * Zoom;
			Child = _Canvas = new Canvas()
			{
				Margin = new Thickness(canvasPadding),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};

			_Random = new Random();
			_Groups = new List<PieceGroup>();
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
			pc.UpdateImage();
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

				double x, y;

				Storyboard storyboard = new Storyboard
				{
					Duration = TimeSpan.FromMilliseconds(500),
					FillBehavior = FillBehavior.Stop,
				};
				storyboard.Completed += (_, __) =>
				{
					_IsAnimating = false;
					PuzzleController?.OnPuzzleRandomized(wasSolved);
				};

				foreach (PuzzlePieceControl ppc in _Canvas.Children.OfType<PuzzlePieceControl>().Where(ppc => !_Groups.Any(g => g.Pieces.Contains(ppc))))
				{
					// Random location for the piece
					x = _Random.NextDouble() * maxXPos;
					y = _Random.NextDouble() * maxYPos;
					ppc.Position = new Point(x, y);

					double xTo = x * Zoom;
					double yTo = y * Zoom;

					var leftAnimator = new DoubleAnimation(xTo, storyboard.Duration, FillBehavior.Stop);
					Storyboard.SetTarget(leftAnimator, ppc);
					Storyboard.SetTargetProperty(leftAnimator, new PropertyPath(Canvas.LeftProperty));
					leftAnimator.Completed += (_, __) => Canvas.SetLeft(ppc, xTo);
					storyboard.Children.Add(leftAnimator);

					var topAnimator = new DoubleAnimation(yTo, storyboard.Duration, FillBehavior.Stop);
					Storyboard.SetTarget(topAnimator, ppc);
					Storyboard.SetTargetProperty(topAnimator, new PropertyPath(Canvas.TopProperty));
					topAnimator.Completed += (_, __) => Canvas.SetTop(ppc, yTo);
					storyboard.Children.Add(topAnimator);
				}

				_IsAnimating = true;
				storyboard.Begin();
			}
		}

		private void OnSolvePuzzle(object sender, EventArgs e)
		{
			if (Pieces != null && Pieces.Any() && !_IsSolved)
			{
				double x, y;

				Storyboard storyboard = new Storyboard
				{
					Duration = TimeSpan.FromMilliseconds(500),
					FillBehavior = FillBehavior.Stop,
				};
				storyboard.Completed += (_, __) =>
				{
					_IsAnimating = false;
					_Groups.Clear();
					_Groups.Add(new PieceGroup(_Canvas.Children.OfType<PuzzlePieceControl>()));
					_IsSolved = true;
					PuzzleController?.OnPuzzleCompleted(true, 0);
				};

				foreach (PuzzlePieceControl ppc in _Canvas.Children.OfType<PuzzlePieceControl>())
				{
					// Solved location for the piece
					x = ppc.X * PuzzlePieceControl.GridSize;
					y = ppc.Y * PuzzlePieceControl.GridSize;
					ppc.Position = new Point(x, y);

					double xTo = x * Zoom;
					double yTo = y * Zoom;

					var leftAnimator = new DoubleAnimation(xTo, storyboard.Duration, FillBehavior.Stop);
					Storyboard.SetTarget(leftAnimator, ppc);
					Storyboard.SetTargetProperty(leftAnimator, new PropertyPath(Canvas.LeftProperty));
					leftAnimator.Completed += (_, __) => Canvas.SetLeft(ppc, xTo);
					storyboard.Children.Add(leftAnimator);

					var topAnimator = new DoubleAnimation(yTo, storyboard.Duration, FillBehavior.Stop);
					Storyboard.SetTarget(topAnimator, ppc);
					Storyboard.SetTargetProperty(topAnimator, new PropertyPath(Canvas.TopProperty));
					topAnimator.Completed += (_, __) => Canvas.SetTop(ppc, yTo);
					storyboard.Children.Add(topAnimator);
				}

				_IsAnimating = true;
				storyboard.Begin();
			}
		}

		private void OnPieceMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (_IsAnimating)
			{
				e.Handled = true;
				return;
			}

			if (sender is PuzzlePieceControl ppc && e.LeftButton == MouseButtonState.Pressed)
			{
				_MovingGroup = FindGroup(ppc);

				if (_MovingGroup != null) BringToTop(_MovingGroup.Pieces);
				else BringToTop(ppc);

				ppc.CaptureMouse();
				_InitialPoint = e.GetPosition(this);

				e.Handled = true;
			}
		}

		private void OnPieceMouseMove(object sender, MouseEventArgs e)
		{
			if (_IsAnimating)
			{
				e.Handled = true;
				return;
			}

			if (sender is PuzzlePieceControl ppc && e.LeftButton == MouseButtonState.Pressed && _InitialPoint.HasValue)
			{
				var pt = e.GetPosition(this);
				double dX = (pt.X - _InitialPoint.Value.X) / Zoom;
				double dY = (pt.Y - _InitialPoint.Value.Y) / Zoom;

				double canvasWidth = _Canvas.ActualWidth / Zoom;
				double canvasHeight = _Canvas.ActualHeight / Zoom;

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
					originalX = x = ppc.Position.X;
					originalY = y = ppc.Position.Y;
					movingItemWidth = ppc.ActualWidth;
					movingItemHeight = ppc.ActualHeight;
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
					_MovingGroup.SetGroupOrigin(x, y, UpdatePosition);
				}
				else
				{
					ppc.Position = new Point(x, y);
					UpdatePosition(ppc);
				}
				
				e.Handled = true;
			}
		}

		private void OnPieceMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (_IsAnimating)
			{
				e.Handled = true;
				return;
			}

			if (sender is PuzzlePieceControl ppc)
			{
				CheckLinks(ppc, _MovingGroup);
				
				ppc.ReleaseMouseCapture();
				
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
			_Canvas.Children.Clear();

			// Add all items in the collection
			if (Pieces != null && Pieces.Any())
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

					var pc = new PuzzlePieceControl(piece)
					{
						Image = Image,
						RenderTransform = _ZoomTransform,
						Position = new Point(x, y),
					};
					pc.MouseDown += OnPieceMouseDown;
					pc.MouseMove += OnPieceMouseMove;
					pc.MouseUp += OnPieceMouseUp;
					_Canvas.Children.Add(pc);
					Panel.SetZIndex(pc, _Canvas.Children.Count);
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

		private void UpdateImage()
		{
			foreach (var pieceControl in _Canvas.Children.OfType<PuzzlePieceControl>())
			{
				pieceControl.Image = Image;
			}
			UpdateBounds();
		}

		private void UpdateBounds()
		{
			_ZoomTransform.ScaleX = _ZoomTransform.ScaleY = Zoom;
			foreach (var ppc in _Canvas.Children.OfType<PuzzlePieceControl>())
			{
				UpdatePosition(ppc);
			}
			double canvasPadding = (PuzzlePieceControl.TabSize + 3) * Zoom;
			_Canvas.Margin = new Thickness(canvasPadding);
			MinWidth = _MaxSizeX * Zoom + _Canvas.Margin.Left + _Canvas.Margin.Right;
			MinHeight = _MaxSizeY * Zoom + _Canvas.Margin.Top + _Canvas.Margin.Bottom;
		}

		private void UpdatePosition(PuzzlePieceControl ppc)
		{
			Canvas.SetLeft(ppc, ppc.Position.X * Zoom);
			Canvas.SetTop(ppc, ppc.Position.Y * Zoom);
		}

		private void BringToTop(PuzzlePieceControl el)
		{
			BringToTop(new PuzzlePieceControl[] { el });
		}

		private void BringToTop(IEnumerable<PuzzlePieceControl> els)
		{
			// Recompute all children ZIndex
			int zIndex = 1;
			foreach (PuzzlePieceControl ppc in _Canvas.Children.OfType<PuzzlePieceControl>().Except(els))
			{
				Panel.SetZIndex(ppc, zIndex++);
			}
			foreach (PuzzlePieceControl ppc in els)
			{
				Panel.SetZIndex(ppc, zIndex++);
			}
		}

		private PieceGroup FindGroup(PuzzlePieceControl ppc)
		{
			return _Groups.FirstOrDefault(g => g.Pieces.Contains(ppc));
		}

		private PuzzlePieceControl FindPiece(int x, int y)
		{
			return _Canvas.Children.OfType<PuzzlePieceControl>().FirstOrDefault(ppc => ppc.X == x && ppc.Y == y);
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
					UpdatePosition(movedPiece);
				}
			}
			else
			{
				if (movedGroup != null)
				{
					movedGroup.AddPiece(neighbor);
					UpdatePosition(neighbor);
				}
				else
				{
					var g = new PieceGroup(neighbor);
					g.AddPiece(movedPiece);
					UpdatePosition(movedPiece);
					_Groups.Add(g);
				}
			}
		}

		private void MergeGroups(PieceGroup group, PieceGroup neighborGroup)
		{
			if (group == neighborGroup) return;

			neighborGroup.MergeWith(group);
			_Groups.Remove(group);
			foreach (var piece in group.Pieces)
			{
				UpdatePosition(piece);
			}
		}

		private bool CheckSolution()
		{
			// There should be a single group...
			if (_Groups.Count != 1) return false;

			// ... with all the pieces
			if (_Canvas.Children.OfType<PuzzlePieceControl>().Except(_Groups[0].Pieces).Any()) return false;

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

			public void SetGroupOrigin(double newOriginX, double newOriginY, Action<PuzzlePieceControl> moveCallback)
			{
				double oldOriginX = Bounds.X;
				double oldOriginY = Bounds.Y;
				double pieceX, pieceY;
				foreach (var groupItem in Pieces)
				{
					pieceX = groupItem.Position.X - oldOriginX + newOriginX;
					pieceY = groupItem.Position.Y - oldOriginY + newOriginY;
					groupItem.Position = new Point(pieceX, pieceY);
					moveCallback.Invoke(groupItem);
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
