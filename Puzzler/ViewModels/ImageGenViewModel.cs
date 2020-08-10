using Puzzler.Controls;
using Puzzler.Models;
using Puzzler.Models.ShaderConfig;
using Puzzler.Services;
using Puzzler.Shaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Puzzler.ViewModels
{
	public class ImageGenViewModel : DependencyObject
	{
		private readonly IDialogService _DialogService;

		public ImageGenViewModel(IDialogService dialogService)
		{
			_DialogService = dialogService;

			ToggleBgColorPopupCommand = new DelegateCommand(() => IsBgColorPopupOpen = !IsBgColorPopupOpen);
			GenerateImageCommand = new DelegateCommand(OnGenerateImage, () => Shader != null && PuzzleSize != null);
			SaveImageCommand = new DelegateCommand(OnSaveImage, () => GeneratedImage != null);
		}

		#region Commands

		public ICommand ToggleBgColorPopupCommand { get; }
		public ICommand GenerateImageCommand { get; }
		public ICommand SaveImageCommand { get; }

		#endregion

		#region Dependency properties

		#region Popups

		public static readonly DependencyProperty IsBgColorPopupOpenProperty = DependencyProperty.Register(nameof(IsBgColorPopupOpen), typeof(bool), typeof(ImageGenViewModel), new PropertyMetadata(false));

		public bool IsBgColorPopupOpen
		{
			get => (bool)GetValue(IsBgColorPopupOpenProperty);
			set => SetValue(IsBgColorPopupOpenProperty, value);
		}

		#endregion

		#region PuzzleSize

		public static readonly DependencyProperty PuzzleSizeProperty = DependencyProperty.Register(nameof(PuzzleSize), typeof(Int32SizeDescriptor), typeof(ImageGenViewModel), new PropertyMetadata(null));

		public Int32SizeDescriptor PuzzleSize
		{
			get => (Int32SizeDescriptor)GetValue(PuzzleSizeProperty);
			set => SetValue(PuzzleSizeProperty, value);
		}

		#endregion

		#region GeneratedImage

		public static readonly DependencyProperty GeneratedImageProperty = DependencyProperty.Register(nameof(GeneratedImage), typeof(BitmapSource), typeof(ImageGenViewModel), new PropertyMetadata(null));

		public BitmapSource GeneratedImage
		{
			get => (BitmapSource)GetValue(GeneratedImageProperty);
			set => SetValue(GeneratedImageProperty, value);
		}

		#endregion

		#region ShaderType

		public static readonly DependencyProperty ShaderProperty = DependencyProperty.Register(nameof(Shader), typeof(ShaderDescriptor), typeof(ImageGenViewModel), new PropertyMetadata(null, OnShaderChanged));

		public ShaderDescriptor Shader
		{
			get => (ShaderDescriptor)GetValue(ShaderProperty);
			set => SetValue(ShaderProperty, value);
		}

		private static void OnShaderChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (ImageGenViewModel)owner;
			foreach (var prop in vm.Shader.Configuration)
			{
				prop.ResetValue();
			}
		}

		#endregion

		#endregion

		#region Event handlers

		private async void OnGenerateImage()
		{
			if (Shader == null || PuzzleSize == null) return;

			// Show progress window
			var controller = await _DialogService.ShowProgressDialogAsync("Generating image...", $"Generating image: {Shader.Description}...", true);

			// Instantiate shader
			var shader = (IShader)Activator.CreateInstance(Shader.Type);

			// Build configuration
			object config = null;
			if (Shader.Configuration != null)
			{
				var configType = shader.ConfigurationType;
				config = Activator.CreateInstance(configType);
				foreach (var prop in Shader.Configuration)
				{
					var propInfo = configType.GetProperty(prop.Key);
					if (propInfo != null)
					{
						propInfo.SetValue(config, prop.CurrentValue);
					}
				}
			}

			// Create bitmap
			int width = PuzzleSize.Width * PuzzlePieceControl.GridSize;
			int height = PuzzleSize.Height * PuzzlePieceControl.GridSize;
			var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
			const int bpp = 4; // bytes per pixel
			byte[] buffer = new byte[width * height * bpp];

			// Render asynchronously
			await Task.Run(() => shader.Render(buffer, width, height, config));

			// Write to bitmap
			bmp.WritePixels(new Int32Rect(0, 0, width, height), buffer, width * bpp, 0);
			bmp.Freeze();

			// Update VM state
			GeneratedImage = bmp;
			CommandManager.InvalidateRequerySuggested();
			controller.Close();
		}

		private async void OnSaveImage()
		{
			string filename = _DialogService.SaveFileDialog("Save generated image...", "PNG images...|*.png");
			if (!string.IsNullOrWhiteSpace(filename))
			{
				var img = GeneratedImage;
				var controller = await _DialogService.ShowProgressDialogAsync("Saving image...", $"Saving image \"{filename}\"...", true, false);
				await Task.Run(() =>
				{
					var pngEncoder = new PngBitmapEncoder();
					pngEncoder.Frames.Add(BitmapFrame.Create(img));
					using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
					{
						pngEncoder.Save(stream);
					}
				});
				controller.Close();
			}
		}

		#endregion
	}
}
