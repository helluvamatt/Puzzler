namespace Puzzler.Models.ShaderConfig
{
	public class NumberUpDownConfigProperty : BaseConfigProperty<int>
	{
		public int Increment { get; set; }
		public int MinValue { get; set; }
		public int MaxValue { get; set; }

		private int _Value;
		public override int Value
		{
			get => _Value;
			set => SetAndNotify(nameof(Value), ref _Value, value);
		}
	}
}
