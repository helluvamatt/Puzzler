using System;

namespace Puzzler.Shaders
{
	public interface IShader
	{
		Type ConfigurationType { get; }
		void Render(byte[] buffer, int w, int h, object config);
	}
}
