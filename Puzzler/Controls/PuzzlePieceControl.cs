using Puzzler.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.Controls
{
	public class PuzzlePieceControl : IComparable<PuzzlePieceControl>
	{
		public const int PieceSize = 70;
		public const int GridSize = 50;
		public const int TabSize = 10;
		private readonly ScaleTransform _ZoomTransform;
		private readonly TransformGroup _Transform;
		private readonly Geometry _Geometry;
		private readonly Geometry _TransformedGeometry;
		private readonly Pen _Pen;
		private readonly Brush _Fill;

		public PuzzlePieceControl(Piece piece, BitmapSource image)
		{
			PositionTransform = new TranslateTransform(0, 0);
			_ZoomTransform = new ScaleTransform(1, 1);
			Piece = piece ?? throw new ArgumentNullException(nameof(piece));

			var imageRegion = new Int32Rect(Piece.X * GridSize - TabSize, Piece.Y * GridSize - TabSize, PieceSize, PieceSize);
			var drawRect = new Rect(-imageRegion.X, -imageRegion.Y, image.Width, image.Height);

			var drawingVisual = new DrawingVisual();
			using (var drawingContext = drawingVisual.RenderOpen())
			{
				drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, imageRegion.Width, imageRegion.Height)));
				drawingContext.DrawImage(image, drawRect);
				drawingContext.Pop();
			}
			var cropped = new RenderTargetBitmap(imageRegion.Width, imageRegion.Height, 96, 96, PixelFormats.Pbgra32);
			cropped.Render(drawingVisual);
			cropped.Freeze();
			_Fill = new ImageBrush
			{
				ImageSource = cropped,
				Viewport = new Rect(-TabSize, -TabSize, PieceSize, PieceSize),
				ViewportUnits = BrushMappingMode.Absolute,
			};
			_Fill.Freeze();
			_Pen = new Pen
			{
				Brush = new SolidColorBrush(Colors.Black),
				LineJoin = PenLineJoin.Round,
				Thickness = 1,
			};
			_Pen.Freeze();
			_Geometry = GetPathGeometry(Piece.NorthConnection, Piece.SouthConnection, Piece.EastConnection, Piece.WestConnection);
			_Geometry.Freeze();
			_TransformedGeometry = _Geometry.Clone();
			_TransformedGeometry.Transform = _Transform = new TransformGroup
			{
				Children = new TransformCollection
				{
					PositionTransform,
					_ZoomTransform
				},
			};
		}

		public Piece Piece { get; }
		public int X => Piece.X;
		public int Y => Piece.Y;

		public double Zoom
		{
			get => _ZoomTransform.ScaleX;
			set
			{
				_ZoomTransform.ScaleX = _ZoomTransform.ScaleY = value;
			}
		}

		public Point Position
		{
			get => new Point(PositionTransform.X, PositionTransform.Y);
			set
			{
				PositionTransform.X = value.X;
				PositionTransform.Y = value.Y;
			}
		}

		public TranslateTransform PositionTransform { get; }

		public void Render(DrawingContext drawingContext)
		{
			drawingContext.PushTransform(_Transform);
			drawingContext.DrawGeometry(_Fill, _Pen, _Geometry);
			drawingContext.Pop();
		}

		public bool HitTest(Point hitPoint)
		{
			return _TransformedGeometry.FillContains(hitPoint);
		}

		#region Private members

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
