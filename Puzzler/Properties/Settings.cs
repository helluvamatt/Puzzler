using System.ComponentModel;

namespace Puzzler.Properties
{
	internal partial class Settings
	{
		protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Save();
			base.OnPropertyChanged(sender, e);
		}
	}
}
