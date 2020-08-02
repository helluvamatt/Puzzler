using Puzzler.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.Controls
{
	public class PuzzlePieceControl : FrameworkElement, IComparable<PuzzlePieceControl>
	{
		public const int PieceSize = 70;
		public const int GridSize = 50;
		public const int TabSize = 10;

		private BitmapSource _Image;
		private Geometry _Geometry;
		private Pen _Pen;
		private Brush _Fill;

		public PuzzlePieceControl(Piece piece)
		{
			Piece = piece ?? throw new ArgumentNullException(nameof(piece));
			Width = Height = GridSize;
			ClipToBounds = false;
			Cursor = Cursors.Hand;
		}

		public Piece Piece { get; }
		public int X => Piece.X;
		public int Y => Piece.Y;

		public Point Position { get; set; }

		public BitmapSource Image
		{
			get => _Image;
			set
			{
				_Image = value;
				BindPiece();
			}
		}

		#region Control overrides

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (_Fill != null && _Pen != null && _Geometry != null)
			{
				drawingContext.DrawGeometry(_Fill, _Pen, _Geometry);
			}
		}

		#endregion

		#region Private members

		private void BindPiece()
		{
			if (_Image != null)
			{
				_Fill = new ImageBrush
				{
					ImageSource = _Image,
					Viewport = new Rect(-TabSize, -TabSize, PieceSize, PieceSize),
					ViewportUnits = BrushMappingMode.Absolute,
					Viewbox = new Rect(Piece.X * GridSize - TabSize, Piece.Y * GridSize - TabSize, PieceSize, PieceSize),
					ViewboxUnits = BrushMappingMode.Absolute,
				};
				_Pen = new Pen
				{
					Brush = new SolidColorBrush(Colors.Black),
					LineJoin = PenLineJoin.Round,
					Thickness = 1,
				};
				_Geometry = GetPathGeometry(Piece.NorthConnection, Piece.SouthConnection, Piece.EastConnection, Piece.WestConnection);
			}
			else
			{
				_Fill = null;
				_Pen = null;
				_Geometry = null;
			}
			InvalidateVisual();
		}

		private static PathGeometry GetPathGeometry(ConnectionType north, ConnectionType south, ConnectionType east, ConnectionType west)
		{
			var northPoints = new List<Point>();
			var eastPoints = new List<Point>();
			var southPoints = new List<Point>();
			var westPoints = new List<Point>();

			for (var i = 0; i < (CURVE.Length / 2); i++)
			{
				northPoints.Add(new Point(CURVE[i * 2], CURVE[i * 2 + 1] * (int)north));
				eastPoints.Add(new Point(GridSize - CURVE[i * 2 + 1] * (int)east, CURVE[i * 2]));
				southPoints.Add(new Point(GridSize - CURVE[i * 2], GridSize - CURVE[i * 2 + 1] * (int)south));
				westPoints.Add(new Point(CURVE[i * 2 + 1] * (int)west, GridSize - CURVE[i * 2]));
			}

			var northSegment = new PolyBezierSegment(northPoints, true);
			var eastSegment = new PolyBezierSegment(eastPoints, true);
			var southSegment = new PolyBezierSegment(southPoints, true);
			var westSegment = new PolyBezierSegment(westPoints, true);

			var pathFigure = new PathFigure()
			{
				IsClosed = true,
				StartPoint = new Point(0, 0)
			};
			pathFigure.Segments.Add(northSegment);
			pathFigure.Segments.Add(eastSegment);
			pathFigure.Segments.Add(southSegment);
			pathFigure.Segments.Add(westSegment);

			var pathGeometry = new PathGeometry();
			pathGeometry.Figures.Add(pathFigure);
			return pathGeometry;
		}

		private static readonly double[] CURVE = new double[]
		{
			0,    0,    17.5, 7.5, 18.5, 2.5,
			18.5, 2.5,  20,   0,   19,   -2.5,
			19,   -2.5, 10,   -10, 25,   -10,
			25,   -10,  40,   -10, 31,   -2.5,
			31,   -2.5, 30,   0,   31.5, 2.5,
			31.5, 2.5,  32.5, 7.5, 50,   0,
		};

		#endregion

		#region IComparable impl

		public int CompareTo(PuzzlePieceControl other)
		{
			if (Y < other.Y) return -1;
			if (Y > other.Y) return 1;
			if (X < other.X) return -1;
			if (X > other.X) return 1;
			return 0;
		}

		#endregion
	}
}
