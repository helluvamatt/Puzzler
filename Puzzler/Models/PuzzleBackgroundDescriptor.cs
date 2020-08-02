using System;
using System.Windows.Media;

namespace Puzzler.Models
{
	public class PuzzleBackgroundDescriptor
	{
		public Guid Key { get; set; }
		public Brush BackgroundBrush { get; set; }
		public string Description { get; set; }
	}
}
