using Puzzler.ViewModels;
using System.Windows.Input;
using System.Windows.Media;

namespace Puzzler.Models.ShaderConfig
{
	public class ColorConfigProperty : BaseConfigProperty<Color>
	{
		public ColorConfigProperty()
		{
			OpenColorPopupCommand = new DelegateCommand(() => IsColorPopupOpen = true);
		}

		private Color _Value;
		public override Color Value
		{
			get => _Value;
			set => SetAndNotify(nameof(Value), ref _Value, value);
		}

		private bool _IsColorPopupOpen;
		public bool IsColorPopupOpen
		{
			get => _IsColorPopupOpen;
			set => SetAndNotify(nameof(IsColorPopupOpen), ref _IsColorPopupOpen, value);
		}

		public ICommand OpenColorPopupCommand { get; }
	}
}
