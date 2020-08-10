namespace Puzzler.Models.ShaderConfig
{
	public class CheckboxConfigProperty : BaseConfigProperty<bool>
	{
		private bool _Value;
		public override bool Value
		{
			get => _Value;
			set => SetAndNotify(nameof(Value), ref _Value, value);
		}
	}
}
