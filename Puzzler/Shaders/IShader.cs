using System.Windows.Media.Imaging;

namespace Puzzler.Shaders
{
	public interface IShader
	{
		void Render(byte[] buffer, int w, int h);
	}
}
