using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Puzzler.Services;
using Puzzler.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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

		private void OnFrameNavigated(object sender, NavigationEventArgs e)
		{
			// Sync state of Hamburger Menu
			hamburgerMenu.IsPaneOpen = false;
			hamburgerMenu.SelectedItem = hamburgerMenu.Items.OfType<MenuItemViewModel>().FirstOrDefault(item => item.NavigationType == e.Content.GetType());
			hamburgerMenu.SelectedOptionsItem = hamburgerMenu.OptionsItems.OfType<MenuItemViewModel>().FirstOrDefault(item => item.NavigationType == e.Content.GetType());

			// Make sure the child view gets our DataContext
			var frame = (Frame)sender;
			if (frame.Content is FrameworkElement content)
			{
				content.DataContext = DataContext;
			}
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

		public string SaveFileDialog(string title, string filters)
		{
			var sfd = new SaveFileDialog()
			{
				Title = title,
				Filter = filters,
				OverwritePrompt = true,
			};
			bool? result = sfd.ShowDialog(this);
			if (result.HasValue && result.Value)
			{
				return sfd.FileName;
			}
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

		public async Task<IProgressController> ShowProgressDialogAsync(string initialTitle, string initialMessage, bool initialIndeterminate = false, bool isCancelable = false)
		{
			var dialogSettings = new MetroDialogSettings
			{
				AnimateHide = false,
				AnimateShow = false,
			};
			var controller = await this.ShowProgressAsync(initialTitle, initialMessage, isCancelable, dialogSettings);
			if (initialIndeterminate) controller.SetIndeterminate();
			return new ProgressController(controller);
		}

		private class ProgressController : IProgressController
		{
			private readonly ProgressDialogController _Pdc;

			private double _LastProgress = 0;

			public ProgressController(ProgressDialogController pdc)
			{
				_Pdc = pdc ?? throw new ArgumentNullException(nameof(pdc));
			}

			public event EventHandler Cancelled
			{
				add => _Pdc.Canceled += value;
				remove => _Pdc.Canceled -= value;
			}

			public async void Close()
			{
				await _Pdc.CloseAsync();
			}

			public void SetIndeterminate(bool indeterminate)
			{
				if (indeterminate) _Pdc.SetIndeterminate();
				else _Pdc.SetProgress(_LastProgress);
			}

			public void SetMessage(string message)
			{
				_Pdc.SetMessage(message);
			}

			public void SetProgress(double progress)
			{
				_Pdc.SetProgress(progress);
				_LastProgress = progress;
			}

			public void SetTitle(string title)
			{
				_Pdc.SetTitle(title);
			}
		}

		#endregion


	}
}
