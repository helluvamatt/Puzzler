namespace Puzzler.Models.ShaderConfig
{
	public class SliderConfigProperty : BaseConfigProperty<double>
	{
		public SliderConfigProperty()
		{
			Increment = 0.01;
			MinValue = 0;
			MaxValue = 1;
			ValueFormat = "{0}";
		}

		public double Increment { get; set; }
		public double MinValue { get; set; }
		public double MaxValue { get; set; }
		public string MinLabel { get; set; }
		public string MaxLabel { get; set; }
		public string ValueFormat { get; set; }

		private double _Value;
		public override double Value
		{
			get => _Value;
			set => SetAndNotify(nameof(Value), ref _Value, value, nameof(FormattedValue));
		}

		public string FormattedValue => string.Format(ValueFormat, Value);
	}
}
