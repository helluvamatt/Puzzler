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
		public const int PieceSize = 140;
		public const int GridSize = 100;
		public const int TabSize = 20;
		private const int Padding = 2;

		private const int Offset = TabSize + Padding;

		private readonly TranslateTransform _PositionTransform;
		private readonly ScaleTransform _ZoomTransform;
		private readonly TransformGroup _Transform;
		private readonly Geometry _TransformedGeometry;
		private readonly BitmapSource _Image;

		public PuzzlePieceControl(Piece piece, BitmapSource image)
		{
			_PositionTransform = new TranslateTransform(-TabSize, -TabSize);
			_ZoomTransform = new ScaleTransform(1, 1);
			Piece = piece ?? throw new ArgumentNullException(nameof(piece));

			// Outline of the puzzle piece
			var pen = new Pen
			{
				Brush = new SolidColorBrush(Colors.Black),
				LineJoin = PenLineJoin.Round,
				Thickness = 1,
			};

			// Geometric shape of the puzzle piece
			var geometry = GetPathGeometry(Piece.NorthConnection, Piece.SouthConnection, Piece.EastConnection, Piece.WestConnection);

			// Transformed geometric shape for hit testing
			_TransformedGeometry = geometry.Clone();
			_TransformedGeometry.Transform = _Transform = new TransformGroup
			{
				Children = new TransformCollection
				{
					_PositionTransform,
					_ZoomTransform
				},
			};

			// Crop image from the main image
			var imageRegion = new Int32Rect(Piece.X * GridSize - TabSize, Piece.Y * GridSize - TabSize, PieceSize, PieceSize);
			var drawRect = new Rect(-imageRegion.X, -imageRegion.Y, image.Width, image.Height);
			var croppedDrawingVisual = new DrawingVisual();
			using (var drawingContext = croppedDrawingVisual.RenderOpen())
			{
				drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, imageRegion.Width, imageRegion.Height)));
				drawingContext.DrawImage(image, drawRect);
				drawingContext.Pop();
			}
			var cropped = new RenderTargetBitmap(imageRegion.Width, imageRegion.Height, 96, 96, PixelFormats.Pbgra32);
			cropped.Render(croppedDrawingVisual);

			// Render the final piece image
			var fill = new ImageBrush
			{
				ImageSource = cropped,
				Viewport = new Rect(-TabSize, -TabSize, PieceSize, PieceSize),
				ViewportUnits = BrushMappingMode.Absolute,
			};
			var drawingVisual = new DrawingVisual();
			using (var drawingContext = drawingVisual.RenderOpen())
			{
				drawingContext.PushTransform(new TranslateTransform(TabSize + Padding, TabSize + Padding));
				drawingContext.DrawGeometry(fill, pen, geometry);
				drawingContext.Pop();
			}
			var finalImage = new RenderTargetBitmap(imageRegion.Width + Padding + Padding, imageRegion.Height + Padding + Padding, 96, 96, PixelFormats.Pbgra32);
			finalImage.Render(drawingVisual);
			finalImage.Freeze();
			_Image = finalImage;
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
			get => new Point(_PositionTransform.X, _PositionTransform.Y);
			set
			{
				_PositionTransform.X = value.X;
				_PositionTransform.Y = value.Y;
			}
		}

		public void Render(DrawingContext drawingContext)
		{
			drawingContext.PushTransform(_Transform);
			drawingContext.DrawImage(_Image, new Rect(-Offset, -Offset, _Image.Width, _Image.Height));
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
			0,  0,   35, 15,  37,  5,
			37, 5,   40, 0,   38,  -5,
			38, -5,  20, -20, 50,  -20,
			50, -20, 80, -20, 62,  -5,
			62, -5,  60, 0,   63,  5,
			63, 5,   65, 15,  100, 0,
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
