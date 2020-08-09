using Puzzler.Services;
using Puzzler.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Puzzler.Controls
{
	public class PuzzleControl : FrameworkElement
	{
		private Point? _InitialPoint = null;
		private PuzzlePieceGroup _MovingGroup = null;
		private PuzzlePieceControl _MovingPiece = null;
		private int _MaxSizeX = 0;
		private int _MaxSizeY = 0;
		private bool _IsAnimating = false;

		#region Dependency properties

		#region Zoom property

		public const double MinZoom = 0.1;
		public const double MaxZoom = 1.0;

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

		#region Puzzle property

		public static readonly DependencyProperty PuzzleProperty = DependencyProperty.Register(nameof(Puzzle), typeof(PuzzleViewModel), typeof(PuzzleControl), new PropertyMetadata(null, OnPuzzleControllerChanged));

		public PuzzleViewModel Puzzle
		{
			get => (PuzzleViewModel)GetValue(PuzzleProperty);
			set => SetValue(PuzzleProperty, value);
		}

		private static void OnPuzzleControllerChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var pc = (PuzzleControl)owner;
			if (e.OldValue is PuzzleViewModel oldController)
			{
				oldController.RandomizePuzzleRequested -= pc.OnRandomizePuzzle;
				oldController.SolvePuzzleRequested -= pc.OnSolvePuzzle;
			}
			if (e.NewValue is PuzzleViewModel newController)
			{
				newController.RandomizePuzzleRequested += pc.OnRandomizePuzzle;
				newController.SolvePuzzleRequested += pc.OnSolvePuzzle;
			}
			pc.BindPieces();
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
			if (Puzzle != null && Puzzle.PieceControls.Any(ppc => ppc.HitTest(e.GetPosition(this))))
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
			else if (Puzzle != null && e.LeftButton == MouseButtonState.Pressed)
			{
				var pt = e.GetPosition(this);
				_MovingPiece = Puzzle.PieceControls.LastOrDefault(ppc => ppc.HitTest(pt));
				if (_MovingPiece != null)
				{
					_MovingGroup = Puzzle.FindGroup(_MovingPiece);

					if (_MovingGroup != null) Puzzle.BringToTop(_MovingGroup);
					else Puzzle.BringToTop(_MovingPiece);
					InvalidateVisual();

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
			else if (_MovingPiece != null && Puzzle != null)
			{
				Puzzle.OnMove(_MovingPiece, _MovingGroup);

				InvalidateVisual();
				ReleaseMouseCapture();

				_InitialPoint = null;
				_MovingGroup = null;
				e.Handled = true;
			}
			base.OnMouseUp(e);
		}

		protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				if (e.Delta > 0)
				{
					// Zoom in
					double zoom = Zoom + 0.1;
					if (zoom > MaxZoom) zoom = MaxZoom;
					Zoom = zoom;
				}
				else if (e.Delta < 0)
				{
					// Zoom out
					double zoom = Zoom - 0.1;
					if (zoom < MinZoom) zoom = MinZoom;
					Zoom = zoom;
				}
			}
			base.OnPreviewMouseWheel(e);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (Puzzle == null) return;

			foreach (PuzzlePieceControl ppc in Puzzle.PieceControls)
			{
				ppc.Render(drawingContext);
			}
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			EnsurePieceBounds();
		}

		#endregion

		#region Event handlers

		private void OnRandomizePuzzle(object sender, EventArgs e)
		{
			if (Puzzle == null || _IsAnimating) return;

			bool wasSolved = Puzzle.IsSolved;
			_MovingGroup = null;

			var animator = new Animator(TimeSpan.FromMilliseconds(500), InvalidateVisual);
			animator.Completed += (_, __) =>
			{
				_IsAnimating = false;
				Puzzle.OnPuzzleRandomized(wasSolved);
			};

			Point pt;
			foreach (PuzzlePieceControl ppc in Puzzle.PieceControls.Where(ppc => !Puzzle.Groups.Any(g => g.Pieces.Contains(ppc))))
			{
				// Random location for the piece
				pt = Puzzle.GetRandomPoint(ActualWidth / Zoom - PuzzlePieceControl.GridSize, ActualHeight / Zoom - PuzzlePieceControl.GridSize);
				animator.AddAnimation(new PointAnimation(ppc.Position, pt, p => ppc.Position = p, Easings.Functions.QuadraticEaseOut));
			}

			_IsAnimating = true;
			animator.Start();
		}

		private void OnSolvePuzzle(object sender, EventArgs e)
		{
			if (Puzzle == null || Puzzle.IsSolved || _IsAnimating) return;

			var animator = new Animator(TimeSpan.FromMilliseconds(500), InvalidateVisual);
			animator.Completed += (_, __) =>
			{
				_IsAnimating = false;
				Puzzle.OnPuzzleCompleted(true);
			};

			Point pt;
			foreach (PuzzlePieceControl ppc in Puzzle.PieceControls)
			{
				// Solved location for the piece
				pt = Puzzle.GetSolvedPoint(ppc);
				animator.AddAnimation(new PointAnimation(ppc.Position, pt, p => ppc.Position = p, Easings.Functions.QuadraticEaseIn));
			}

			_IsAnimating = true;
			animator.Start();
		}

		#endregion

		#region Private members

		private void BindPieces()
		{
			if (Puzzle != null)
			{
				// Based on maxX and maxY, determine size of canvas
				_MaxSizeX = (Puzzle.MaxX + 3) * PuzzlePieceControl.GridSize;
				_MaxSizeY = (Puzzle.MaxY + 3) * PuzzlePieceControl.GridSize;
			}
			else
			{
				_MaxSizeX = 0;
				_MaxSizeY = 0;
			}

			// Compute and update bounds for all pieces (if present)
			UpdateBounds();
		}

		private void UpdateBounds()
		{
			if (Puzzle != null)
			{
				foreach (var ppc in Puzzle.PieceControls)
				{
					ppc.Zoom = Zoom;
				}
			}
			MinWidth = _MaxSizeX * Zoom;
			MinHeight = _MaxSizeY * Zoom;
			EnsurePieceBounds();
			InvalidateVisual();
		}

		private void EnsurePieceBounds()
		{
			if (Puzzle != null && ActualWidth > 0 && ActualHeight > 0 && Zoom > 0)
			{
				double canvasWidth = ActualWidth / Zoom;
				double canvasHeight = ActualHeight / Zoom;
				double originalX, originalY, x, y, itemWidth, itemHeight;

				// Ensure groups are within the bounds
				foreach (PuzzlePieceGroup group in Puzzle.Groups)
				{
					Rect bounds = group.Bounds;
					originalX = x = bounds.X;
					originalY = y = bounds.Y;
					itemWidth = bounds.Width;
					itemHeight = bounds.Height;

					if (x > canvasWidth - itemWidth) x = canvasWidth - itemWidth;
					if (y > canvasHeight - itemHeight) y = canvasHeight - itemHeight;

					if (x < originalX || y < originalY) group.SetGroupOrigin(x, y);
				}

				// Ensure pieces not in groups are within the bounds
				foreach (PuzzlePieceControl ppc in Puzzle.PieceControls.Except(Puzzle.Groups.SelectMany(g => g.Pieces)))
				{
					Point pos = ppc.Position;
					originalX = x = pos.X;
					originalY = y = pos.Y;
					itemWidth = PuzzlePieceControl.GridSize;
					itemHeight = PuzzlePieceControl.GridSize;

					if (x > canvasWidth - itemWidth) x = canvasWidth - itemWidth;
					if (y > canvasHeight - itemHeight) y = canvasHeight - itemHeight;

					if (x < originalX || y < originalY) ppc.Position = new Point(x, y);
				}

				InvalidateVisual();
			}
		}

		#endregion
	}
}
