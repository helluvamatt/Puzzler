using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.Shaders
{
	/// <summary>
	/// Renders a color wheel to the image
	/// </summary>
	public class ColorWheelGradientShader : IShader
	{
		private const double RAD_TO_DEG = 57.295779513082320876798154814105;

		public void Render(byte[] buffer, int w, int h)
		{
			int cX = w / 2;
			int cY = h / 2;
			int i = 0;

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					double angle = Math.Atan2(y - cY, x - cX) * RAD_TO_DEG;

					// Clamp
					while (angle < 0) angle += 360;
					while (angle > 360) angle -= 360;

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
}
