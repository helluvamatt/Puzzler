using Puzzler.Services;
using System.Collections.Generic;
using System.ComponentModel;

namespace Puzzler.ViewModels
{
	public class MessageDialogViewModel : INotifyPropertyChanged
	{
		private string _Title;
		private string _Message;
		private MessageBoxType _IconType;
		private string _DetailedMessage;
		
		public string Title
		{
			get => _Title;
			set => ChangeAndNotify(nameof(Title), ref _Title, value);
		}

		public string Message
		{
			get => _Message;
			set => ChangeAndNotify(nameof(Message), ref _Message, value);
		}

		public MessageBoxType IconType
		{
			get => _IconType;
			set => ChangeAndNotify(nameof(IconType), ref _IconType, value);
		}

		public string DetailedMessage
		{
			get => _DetailedMessage;
			set => ChangeAndNotify(nameof(DetailedMessage), ref _DetailedMessage, value);
		}

		private void ChangeAndNotify<T>(string propertyName, ref T storage, T value)
		{
			if (!EqualityComparer<T>.Default.Equals(storage, value))
			{
				storage = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
