using Puzzler.Controls;
using Puzzler.Models;
using Puzzler.Services;
using Puzzler.Shaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
			GenerateImageCommand = new DelegateCommand(OnGenerateImage, () => ShaderType != null && PuzzleSize != null);
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

		#region BackgroundColor

		public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(nameof(BackgroundColor), typeof(Color), typeof(ImageGenViewModel), new PropertyMetadata(Colors.Black));

		public Color BackgroundColor
		{
			get => (Color)GetValue(BackgroundColorProperty);
			set => SetValue(BackgroundColorProperty, value);
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

		public static readonly DependencyProperty ShaderTypeProperty = DependencyProperty.Register(nameof(ShaderType), typeof(Type), typeof(ImageGenViewModel), new PropertyMetadata(null));

		public Type ShaderType
		{
			get => (Type)GetValue(ShaderTypeProperty);
			set => SetValue(ShaderTypeProperty, value);
		}

		#endregion

		#endregion

		#region Event handlers

		private async void OnGenerateImage()
		{
			if (ShaderType == null || PuzzleSize == null) return;

			// Instantiate shader
			var shader = (IShader)Activator.CreateInstance(ShaderType);

			// Create bitmap
			int width = PuzzleSize.Width * PuzzlePieceControl.GridSize;
			int height = PuzzleSize.Height * PuzzlePieceControl.GridSize;
			var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
			const int bpp = 4; // bytes per pixel
			byte[] buffer = new byte[width * height * bpp];

			// Render asynchronously
			await Task.Run(() => shader.Render(buffer, width, height));

			// Write to bitmap
			bmp.WritePixels(new Int32Rect(0, 0, width, height), buffer, width * bpp, 0);
			bmp.Freeze();
			GeneratedImage = bmp;
			CommandManager.InvalidateRequerySuggested();
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
