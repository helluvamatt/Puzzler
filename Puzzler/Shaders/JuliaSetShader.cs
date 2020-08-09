namespace Puzzler.Shaders
{
	public class JuliaSetShader : IShader
	{
		public void Render(byte[] buffer, int w, int h)
		{
            // TODO Parameterize zoom, moveX, moveY, hueOffset
            // TODO Optionally parameterize cX, cY
            const int zoom = 1;
            const int maxiter = 255;
            const int moveX = 0;
            const int moveY = 0;
            const double hueOffset = 0.0;
            const double cX = -0.7;
            const double cY = 0.27015;
            double zx, zy, tmp;
            int i, p;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    zx = 1.5 * (x - w / 2) / (0.5 * zoom * w) + moveX;
                    zy = 1.0 * (y - h / 2) / (0.5 * zoom * h) + moveY;
                    i = maxiter;
                    while (zx * zx + zy * zy < 4 && i > 1)
                    {
                        tmp = zx * zx - zy * zy + cX;
                        zy = 2.0 * zx * zy + cY;
                        zx = tmp;
                        i -= 1;
                    }

                    // set pixel
                    Utils.HsvToRgb(Rotate((double)i / maxiter * 360.0, hueOffset, 360.0), 1, i, out byte r, out byte g, out byte b);
                    p = (y * w + x) * 4;
                    buffer[p + 0] = b; // B
                    buffer[p + 1] = g; // G
                    buffer[p + 2] = r; // R
                    buffer[p + 3] = 0xFF; // A
                }
            }
        }

        private static double Rotate(double val, double offset, double max)
		{
            val += offset;
            while (val < 0) val += max;
            while (val > max) val -= max;
            return val;
		}
	}
}
