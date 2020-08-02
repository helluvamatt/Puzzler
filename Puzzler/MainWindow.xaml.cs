using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Puzzler.Services;
using Puzzler.ViewModels;
using System.Threading.Tasks;
using System.Windows;

namespace Puzzler
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow, IDialogService, IWindowResourceService
	{
		private readonly CustomDialog _MessageDialog;
		private readonly MessageDialogViewModel _MessageDialogViewModel;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = new ShellViewModel(this, this);
			_MessageDialogViewModel = new MessageDialogViewModel();
			_MessageDialog = (CustomDialog)Resources["messageDialog"];
			_MessageDialog.DataContext = _MessageDialogViewModel;
		}

		#region IDialogService

		public async Task<bool?> ShowConfirmDialogAsync(string title, string message, string affirmativeButtonText = null, string negativeButtonText = null)
		{
			var dialogSettings = new MetroDialogSettings
			{
				AffirmativeButtonText = affirmativeButtonText ?? "Yes",
				NegativeButtonText = negativeButtonText ?? "No",
				AnimateHide = false,
				AnimateShow = false,
			};
			var result = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
			if (result == MessageDialogResult.Affirmative) return true;
			if (result == MessageDialogResult.Negative) return false;
			return null;
		}

		public string OpenFileDialog(string title, string filters)
		{
			var ofd = new OpenFileDialog()
			{
				Title = title,
				Filter = filters
			};
			bool? result = ofd.ShowDialog(this);
			if (result.HasValue && result.Value)
			{
				return ofd.FileName;
			}
			return null;
		}

		public async void ShowMessageBox(string title, string message, MessageBoxType type, string detailedMessage = null)
		{
			_MessageDialogViewModel.Title = title;
			_MessageDialogViewModel.Message = message;
			_MessageDialogViewModel.IconType = type;
			_MessageDialogViewModel.DetailedMessage = detailedMessage;
			var dialogSettings = new MetroDialogSettings
			{
				AnimateHide = false,
				AnimateShow = false,
			};
			await this.ShowMetroDialogAsync(_MessageDialog, dialogSettings);
		}

		private async void OnCustomDialogOKClick(object sender, RoutedEventArgs e)
		{
			var dialogSettings = new MetroDialogSettings
			{
				AnimateHide = false,
				AnimateShow = false,
			};
			await this.HideMetroDialogAsync(_MessageDialog, dialogSettings);
		}

		#endregion
	}
}
