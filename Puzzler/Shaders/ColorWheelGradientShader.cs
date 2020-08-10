using System;

namespace Puzzler.Shaders
{
	/// <summary>
	/// Renders a color wheel to the image
	/// </summary>
	public class ColorWheelGradientShader : IShader
	{
		private const double RAD_TO_DEG = 57.295779513082320876798154814105;

		public Type ConfigurationType => typeof(ColorWheelGradientShaderConfig);

		public void Render(byte[] buffer, int w, int h, object config)
		{
			var cfg = (ColorWheelGradientShaderConfig)config;
			int cX = w / 2;
			int cY = h / 2;
			int i = 0;

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					double angle = Utils.Rotate(Math.Atan2(y - cY, x - cX) * RAD_TO_DEG, cfg.Angle, 360);

					// Get color
					Utils.HsvToRgb(angle, 1, 1, out byte r, out byte g, out byte b);
					buffer[i++] = b;
					buffer[i++] = g;
					buffer[i++] = r;
					buffer[i++] = 0xff;
				}
			}
		}
	}

	public class ColorWheelGradientShaderConfig
	{
		public double Angle { get; set; }
	}
}
