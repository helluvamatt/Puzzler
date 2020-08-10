using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.Shaders
{
	public class RandomSpheresShader : IShader
	{
		public Type ConfigurationType => typeof(RandomSpheresShaderConfig);

		public void Render(byte[] buffer, int w, int h, object config)
		{
			var cfg = (RandomSpheresShaderConfig)config;
			var rng = new Random();
			var drawing = new DrawingVisual();
			using (var ctxt = drawing.RenderOpen())
			{
				// Fill with background
				ctxt.DrawRectangle(new SolidColorBrush(cfg.BackgroundColor), null, new Rect(0, 0, w, h));

				double minCX = w * 0.1;
				double minCY = h * 0.1;
				double maxCX = w * 0.9;
				double maxCY = h * 0.9;
				double minR = Math.Min(w, h) * 0.05;
				double maxR = Math.Max(w, h) * cfg.MaxSphereSize;

				// Render N spheres
				for (int i = 0; i < cfg.SphereCount; i++)
				{
					// Pick center point
					var center = new Point
					{
						X = rng.NextDouble().InRange(minCX, maxCX),
						Y = rng.NextDouble().InRange(minCY, maxCY)
					};

					// Pick radius
					double radius = rng.NextDouble().InRange(minR, maxR);

					// Determine the color and brush
					double hue = rng.NextDouble().InRange(0, 360);
					double sat = rng.NextDouble().InRange(0.75, 1.0);
					double val = rng.NextDouble().InRange(0.75, 1.0);
					Utils.HsvToRgb(hue, sat, val, out byte r, out byte g, out byte b);
					var color = Color.FromRgb(r, g, b);
					var brush = new RadialGradientBrush()
					{
						GradientOrigin = new Point(0.7, 0.3),
					};
					brush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
					brush.GradientStops.Add(new GradientStop(color, 1.0));

					// Draw the circle/sphere
					ctxt.DrawEllipse(brush, null, center, radius, radius);
				}
			}
			var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
			rtb.Render(drawing);
			rtb.CopyPixels(buffer, w * 4, 0);
		}
	}

	public class RandomSpheresShaderConfig
	{
		public Color BackgroundColor { get; set; }
		public double MaxSphereSize { get; set; }
		public int SphereCount { get; set; }
	}

	internal static class RandomExt
	{
		public static double InRange(this double val, double min, double max)
		{
			if (max < min) throw new ArgumentOutOfRangeException("Max must be greater than or equal to min.");
			return val * (max - min) + min;
		}
	}
}
