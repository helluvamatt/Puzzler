using System.Collections.Generic;
using System.ComponentModel;

namespace Puzzler.Models.ShaderConfig
{
	public interface IConfigProperty
	{
		string Key { get; }
		object CurrentValue { get; set; }
		void ResetValue();
	}

	public abstract class BaseConfigProperty<T> : IConfigProperty, INotifyPropertyChanged
	{
		public string Key { get; set; }
		public string Label { get; set; }
		public T DefaultValue { get; set; }

		public abstract T Value { get; set; }

		#region IConfigProperty impl

		public object CurrentValue
		{
			get => Value;
			set => Value = (T)value;
		}

		public void ResetValue()
		{
			Value = DefaultValue;
		}

		#endregion

		#region INotifyPropertyChanged impl

		protected void SetAndNotify<TProp>(string propName, ref TProp storage, TProp value, params string[] dependenentProperties)
		{
			if (!EqualityComparer<TProp>.Default.Equals(storage, value))
			{
				storage = value;
				Notify(propName);
				foreach (string otherName in dependenentProperties)
				{
					Notify(otherName);
				}
			}
		}

		protected void Notify(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
