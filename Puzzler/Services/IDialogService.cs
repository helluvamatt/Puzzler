using System.Threading.Tasks;

namespace Puzzler.Services
{
	public interface IDialogService
	{
		string OpenFileDialog(string title, string filters);
		void ShowMessageBox(string title, string message, MessageBoxType type, string detailedMessage = null);
		Task<bool?> ShowConfirmDialogAsync(string title, string message, string affirmativeButtonText = null, string negativeButtonText = null);
	}

	public enum MessageBoxType
	{
		Error,
		Info,
		None
	}
}
