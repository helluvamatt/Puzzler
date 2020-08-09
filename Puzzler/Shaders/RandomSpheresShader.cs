using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.Shaders
{
	public class RandomSpheresShader : IShader
	{
		const int NumberBlobs = 25;

		public void Render(byte[] buffer, int w, int h)
		{
			var rng = new Random();
			var drawing = new DrawingVisual();
			using (var ctxt = drawing.RenderOpen())
			{
				// Fill with background
				var bgColor = Color.FromArgb(buffer[3], buffer[2], buffer[1], buffer[0]);
				ctxt.DrawRectangle(new SolidColorBrush(bgColor), null, new Rect(0, 0, w, h));

				double minCX = w * 0.1;
				double minCY = h * 0.1;
				double maxCX = w * 0.9;
				double maxCY = h * 0.9;
				double minR = Math.Min(w, h) * 0.05;
				double maxR = Math.Max(w, h) * 0.2;

				// Render N spheres
				for (int i = 0; i < NumberBlobs; i++)
				{
					// Pick center point
					double cX = rng.NextDoubleInRange(minCX, maxCX);
					double cY = rng.NextDoubleInRange(minCY, maxCY);

					// Pick radius
					double radius = rng.NextDoubleInRange(minR, maxR);

					// Determine the color and brush
					double hue = rng.NextDoubleInRange(0, 360);
					double sat = rng.NextDoubleInRange(0.75, 1.0);
					double val = rng.NextDoubleInRange(0.75, 1.0);
					Utils.HsvToRgb(hue, sat, val, out byte r, out byte g, out byte b);
					var color = Color.FromRgb(r, g, b);
					var brush = new RadialGradientBrush()
					{
						GradientOrigin = new Point(0.7, 0.3),
					};
					brush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
					brush.GradientStops.Add(new GradientStop(color, 1.0));

					// Draw the circle/sphere
					ctxt.DrawEllipse(brush, null, new Point(cX, cY), radius, radius);
				}
			}
			var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
			rtb.Render(drawing);
			rtb.CopyPixels(buffer, w * 4, 0);
		}
	}

	internal static class RandomExt
	{
		public static double NextDoubleInRange(this Random rng, double min, double max)
		{
			return rng.NextDouble() * (max - min) + min;
		}
	}
}
