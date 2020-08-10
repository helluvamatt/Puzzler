using Puzzler.Models.ShaderConfig;
using System;
using System.Windows.Markup;

namespace Puzzler.Models
{
	[ContentProperty(nameof(Configuration))]
	public class ShaderDescriptor
	{
		public ShaderDescriptor()
		{
			Configuration = new ShaderConfigPropertyCollection();
		}

		public string Description { get; set; }
		public Type Type { get; set; }

		public ShaderConfigPropertyCollection Configuration { get; }
	}
}
