namespace Puzzler.Models
{
	public class Int32SizeDescriptor
	{
		public int Width { get; set; }
		public int Height { get; set; }

		public string Description => $"{Width * Height} pieces ({Width} x {Height})";

		public override bool Equals(object obj) => obj is Int32SizeDescriptor other && other.Width == Width && other.Height == Height;
		public override int GetHashCode() => Description.GetHashCode();
		public override string ToString() => Description;
	}
}
