using System;

namespace Puzzler.Shaders
{
	public class JuliaSetShader : IShader
	{
        public Type ConfigurationType => typeof(JuliaSetShaderConfig);

		public void Render(byte[] buffer, int w, int h, object config)
		{
            var cfg = (JuliaSetShaderConfig)config;
            const int maxiter = 255;
            const double cX = -0.7;
            const double cY = 0.27015;
            double zx, zy, tmp;
            int i, p;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    zx = 1.5 * (x - w / 2) / (0.5 * cfg.Zoom * w) + cfg.MoveX;
                    zy = 1.0 * (y - h / 2) / (0.5 * cfg.Zoom * h) + cfg.MoveY;
                    i = maxiter;
                    while (zx * zx + zy * zy < 4 && i > 1)
                    {
                        tmp = zx * zx - zy * zy + cX;
                        zy = 2.0 * zx * zy + cY;
                        zx = tmp;
                        i -= 1;
                    }

                    // set pixel
                    Utils.HsvToRgb(Utils.Rotate((double)i / maxiter * 360.0, cfg.HueOffset, 360.0), 1, i, out byte r, out byte g, out byte b);
                    p = (y * w + x) * 4;
                    buffer[p + 0] = b; // B
                    buffer[p + 1] = g; // G
                    buffer[p + 2] = r; // R
                    buffer[p + 3] = 0xFF; // A
                }
            }
        }
	}

    public class JuliaSetShaderConfig
	{
        public double HueOffset { get; set; }
        public double Zoom { get; set; }
        public double MoveX { get; set; }
        public double MoveY { get; set; }
    }
}
