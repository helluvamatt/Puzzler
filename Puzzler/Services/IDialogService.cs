using System;
using System.Threading.Tasks;

namespace Puzzler.Services
{
	public interface IDialogService
	{
		string OpenFileDialog(string title, string filters);
		string SaveFileDialog(string title, string filters);
		void ShowMessageBox(string title, string message, MessageBoxType type, string detailedMessage = null);
		Task<bool?> ShowConfirmDialogAsync(string title, string message, string affirmativeButtonText = null, string negativeButtonText = null);
		Task<IProgressController> ShowProgressDialogAsync(string initialTitle, string initialMessage, bool initialIndeterminate = false, bool isCancelable = false);
	}

	public interface IProgressController
	{
		void SetTitle(string title);
		void SetMessage(string message);
		void SetProgress(double progress);
		void SetIndeterminate(bool indeterminate);
		void Close();

		event EventHandler Cancelled;
	}

	public enum MessageBoxType
	{
		Error,
		Info,
		None
	}
}
