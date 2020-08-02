using Puzzler.Controls;
using Puzzler.Models;
using Puzzler.Properties;
using Puzzler.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Puzzler.ViewModels
{
	public class ShellViewModel : DependencyObject, IPuzzleController
	{
		private readonly IDialogService _DialogService;
		private readonly IWindowResourceService _ResourceService;
		private readonly DispatcherTimer _Timer;

		private DateTime? _TimerStart;

		public ShellViewModel(IDialogService dialogService, IWindowResourceService resourceService)
		{
			_DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
			_ResourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
			_Timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
			{
				Interval = TimeSpan.FromMilliseconds(50),
			};
			_Timer.Tick += OnTimerTick;

			Settings.Default.PropertyChanged += OnSettingsPropertyChanged;

			OpenSettingsCommand = new DelegateCommand(() => IsSettingsFlyoutOpen = true);
			OpenCreatePuzzleCommand = new DelegateCommand(() => IsCreatePuzzleFlyoutOpen = true);
			OpenImageCommand = new DelegateCommand(OnOpenImage);
			GeneratePuzzleCommand = new DelegateCommand(OnGeneratePuzzle, () => PreviewImage != null && PuzzleSize != null);
			SolvePuzzleCommand = new DelegateCommand(OnSolvePuzzle, () => Pieces != null && Pieces.Any());
			RandomizePuzzleCommand = new DelegateCommand(OnRandomizePuzzle, () => Pieces != null && Pieces.Any());

			PuzzleSize = new Int32SizeDescriptor { Width = Settings.Default.PieceHorizontalCount, Height = Settings.Default.PieceVerticalCount };

			// Load background from settings
			if (_ResourceService.FindResource("puzzleBackgrounds") is PuzzleBackgroundDescriptor[] backgrounds)
			{
				PuzzleBackground = backgrounds.FirstOrDefault(bg => bg.Key == Settings.Default.PuzzleBackground);

				// If the setting is invalid, just use the first one in the list
				if (PuzzleBackground == null) PuzzleBackground = backgrounds.FirstOrDefault();
			}
		}

		#region Commands

		public ICommand OpenSettingsCommand { get; }
		public ICommand OpenCreatePuzzleCommand { get; }
		public ICommand OpenImageCommand { get; }
		public ICommand GeneratePuzzleCommand { get; }
		public ICommand SolvePuzzleCommand { get; }
		public ICommand RandomizePuzzleCommand { get; }

		#endregion

		#region Dependency properties

		#region IsCreatePuzzleFlyoutOpen

		public static readonly DependencyProperty IsCreatePuzzleFlyoutOpenProperty = DependencyProperty.Register(nameof(IsCreatePuzzleFlyoutOpen), typeof(bool), typeof(ShellViewModel), new PropertyMetadata(true));

		public bool IsCreatePuzzleFlyoutOpen
		{
			get => (bool)GetValue(IsCreatePuzzleFlyoutOpenProperty);
			set => SetValue(IsCreatePuzzleFlyoutOpenProperty, value);
		}

		#endregion

		#region IsSettingsFlyoutOpen

		public static readonly DependencyProperty IsSettingsFlyoutOpenProperty = DependencyProperty.Register(nameof(IsSettingsFlyoutOpen), typeof(bool), typeof(ShellViewModel), new PropertyMetadata(false));

		public bool IsSettingsFlyoutOpen
		{
			get => (bool)GetValue(IsSettingsFlyoutOpenProperty);
			set => SetValue(IsSettingsFlyoutOpenProperty, value);
		}

		#endregion

		#region PreviewImage

		public static readonly DependencyProperty PreviewImageProperty = DependencyProperty.Register(nameof(PreviewImage), typeof(BitmapSource), typeof(ShellViewModel), new PropertyMetadata(null));

		public BitmapSource PreviewImage
		{
			get => (BitmapSource)GetValue(PreviewImageProperty);
			set => SetValue(PreviewImageProperty, value);
		}

		#endregion

		#region PuzzleImage

		public static readonly DependencyProperty PuzzleImageProperty = DependencyProperty.Register(nameof(PuzzleImage), typeof(BitmapSource), typeof(ShellViewModel), new PropertyMetadata(null));

		public BitmapSource PuzzleImage
		{
			get => (BitmapSource)GetValue(PuzzleImageProperty);
			set => SetValue(PuzzleImageProperty, value);
		}

		#endregion

		#region CurrentImageSource

		public static readonly DependencyProperty CurrentImageSourceProperty = DependencyProperty.Register(nameof(CurrentImageSource), typeof(string), typeof(ShellViewModel), new PropertyMetadata(null));

		public string CurrentImageSource
		{
			get => (string)GetValue(CurrentImageSourceProperty);
			set => SetValue(CurrentImageSourceProperty, value);
		}

		#endregion

		#region PuzzleSize

		public static readonly DependencyProperty PuzzleSizeProperty = DependencyProperty.Register(nameof(PuzzleSize), typeof(Int32SizeDescriptor), typeof(ShellViewModel), new PropertyMetadata(null));

		public Int32SizeDescriptor PuzzleSize
		{
			get => (Int32SizeDescriptor)GetValue(PuzzleSizeProperty);
			set => SetValue(PuzzleSizeProperty, value);
		}

		#endregion

		#region PuzzleBackground

		public static readonly DependencyProperty PuzzleBackgroundProperty = DependencyProperty.Register(nameof(PuzzleBackground), typeof(PuzzleBackgroundDescriptor), typeof(ShellViewModel), new PropertyMetadata(null));

		public PuzzleBackgroundDescriptor PuzzleBackground
		{
			get => (PuzzleBackgroundDescriptor)GetValue(PuzzleBackgroundProperty);
			set => SetValue(PuzzleBackgroundProperty, value);
		}

		#endregion

		#region Zoom

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(ShellViewModel), new PropertyMetadata(1.0));

		public double Zoom
		{
			get => (double)GetValue(ZoomProperty);
			set => SetValue(ZoomProperty, value);
		}

		#endregion

		#region Pieces

		public static readonly DependencyProperty PiecesProperty = DependencyProperty.Register(nameof(Pieces), typeof(IEnumerable<Piece>), typeof(ShellViewModel), new PropertyMetadata(null));

		public IEnumerable<Piece> Pieces
		{
			get => (IEnumerable<Piece>)GetValue(PiecesProperty);
			set => SetValue(PiecesProperty, value);
		}

		#endregion

		#region MoveCount

		public static readonly DependencyProperty MoveCountProperty = DependencyProperty.Register(nameof(MoveCount), typeof(int), typeof(ShellViewModel), new PropertyMetadata(0));

		public int MoveCount
		{
			get => (int)GetValue(MoveCountProperty);
			set => SetValue(MoveCountProperty, value);
		}

		#endregion

		#region Timer

		public static readonly DependencyProperty TimerProperty = DependencyProperty.Register(nameof(Timer), typeof(TimeSpan), typeof(ShellViewModel), new PropertyMetadata(TimeSpan.Zero));

		public TimeSpan Timer
		{
			get => (TimeSpan)GetValue(TimerProperty);
			set => SetValue(TimerProperty, value);
		}

		#endregion

		#endregion

		#region IPuzzleController impl

		public event EventHandler SolvePuzzle;
		public event EventHandler RandomizePuzzle;

		public void OnPuzzleRandomized(bool wasSolved)
		{
			if (wasSolved) ResetTimer();
		}

		public void OnPuzzleCompleted(bool autoSolved, int moveCount)
		{
			// Stop timer
			if (Settings.Default.ShowTimer) _Timer.Stop();
			UpdateTimer();
			_TimerStart = null;

			// Show completed message and statistics
			if (!autoSolved)
			{
				int moves = MoveCount;
				int pieces = PuzzleSize.Width * PuzzleSize.Height;
				float avgMovesPerPiece = moves / (float)pieces;

				var statisticsBuilder = new StringBuilder();
				statisticsBuilder.AppendLine($"Pieces Count:       {pieces}");
				statisticsBuilder.AppendLine($"Move Count:         {moves}");
				statisticsBuilder.AppendLine($"Total Time:         {Timer}");
				statisticsBuilder.AppendLine($"Avg. Moves / Piece: {avgMovesPerPiece}");

				_DialogService.ShowMessageBox("Complete!", "You have completed the puzzle!", MessageBoxType.Info, statisticsBuilder.ToString().TrimEnd());
			}
		}

		#endregion

		#region Event handlers

		private async void OnOpenImage()
		{
			string result = _DialogService.OpenFileDialog("Select an image...", "Image files...|*.png;*.jpg;*.jpeg;*.bmp|PNG images...|*.png|JPEG images...|*.jpeg;*.jpg|BMP images...|*.bmp|All files...|*.*");
			if (!string.IsNullOrWhiteSpace(result))
			{
				Exception exception = null;
				var controller = await _DialogService.ShowProgressDialogAsync("Loading image...", $"Loading \"{result}\"...", true);
				PreviewImage = await Task.Run<BitmapSource>(() =>
				{
					try
					{
						// Load original image
						var rawImage = new BitmapImage();
						using (var fileStream = new FileStream(result, FileMode.Open, FileAccess.Read))
						{
							rawImage.BeginInit();
							rawImage.StreamSource = fileStream;
							rawImage.CacheOption = BitmapCacheOption.OnLoad;
							rawImage.EndInit();
							rawImage.Freeze();
						}

						// If the DPI is 96, we can use the image directly
						if (rawImage.DpiX == 96 && rawImage.DpiY == 96) return rawImage;

						// Otherwise, it needs to be converted
						var drawingVisual = new DrawingVisual();
						var imageBounds = new Rect(0, 0, rawImage.PixelWidth, rawImage.PixelHeight);
						using (var drawingContext = drawingVisual.RenderOpen())
						{
							drawingContext.PushClip(new RectangleGeometry(imageBounds));
							drawingContext.DrawImage(rawImage, imageBounds);
							drawingContext.Pop();
						}
						var rtb = new RenderTargetBitmap(rawImage.PixelWidth, rawImage.PixelHeight, 96, 96, PixelFormats.Pbgra32);
						rtb.Render(drawingVisual);
						rtb.Freeze();
						return rtb;
					}
					catch (Exception ex)
					{
						exception = ex;
						return null;
					}
				});

				controller.Close();

				if (exception != null)
				{
					_DialogService.ShowMessageBox("Failed to load image...", exception.Message, MessageBoxType.Error, exception.ToString());
				}
				else
				{
					CurrentImageSource = result;
					CommandManager.InvalidateRequerySuggested();
				}
			}
		}

		private void OnGeneratePuzzle()
		{
			if (PreviewImage != null && PuzzleSize != null)
			{
				var rng = new Random();

				int hCount = PuzzleSize.Width;
				int vCount = PuzzleSize.Height;
				var originalImage = PreviewImage;
				var fillType = Settings.Default.PuzzleImageFillType;

				// Determine puzzle image bounds
				int width = hCount * PuzzlePieceControl.GridSize;
				int height = vCount * PuzzlePieceControl.GridSize;
				var bounds = new Rect(0, 0, width, height);

				// Scale source image down to fit bounds
				double realScale = Math.Max((double)width / originalImage.PixelWidth, (double)height / originalImage.PixelHeight);
				double scale = Math.Min(1.0, realScale);
				double scaledWidth = originalImage.PixelWidth * scale;
				double scaledHeight = originalImage.PixelHeight * scale;
				var imgRect = new Rect((width - scaledWidth) / 2.0, (height - scaledHeight) / 2.0, scaledWidth, scaledHeight);

				// Prepare to draw scaled image
				DrawingVisual visual = new DrawingVisual();
				DrawingContext context = visual.RenderOpen();
				context.PushClip(new RectangleGeometry(bounds));

				if (realScale > 1.0)
				{
					// Draw background
					Brush backgroundBrush;
					switch (fillType)
					{
						case BackgroundFillType.RainbowSpiral:
							var gsc = new GradientStopCollection
							{
								new GradientStop(Color.FromRgb(0xFF, 0,    0),    0.0),
								new GradientStop(Color.FromRgb(0xFF, 0xFF, 0),    0.166666666),
								new GradientStop(Color.FromRgb(0,    0xFF, 0),    0.333333333),
								new GradientStop(Color.FromRgb(0,    0xFF, 0xFF), 0.5),
								new GradientStop(Color.FromRgb(0,    0,    0xFF), 0.666666666),
								new GradientStop(Color.FromRgb(0xFF, 0,    0xFF), 0.833333333),
								new GradientStop(Color.FromRgb(0xFF, 0,    0),    1.0)
							};
							backgroundBrush = new RadialGradientBrush(gsc)
							{
								MappingMode = BrushMappingMode.RelativeToBoundingBox,
								Center = new Point(0.5, 0.5),
								RadiusX = 0.707106781, // cos(45º)
								RadiusY = 0.707106781, // sin(45º)
							};
							break;
						case BackgroundFillType.White:
							backgroundBrush = new SolidColorBrush(Colors.White);
							break;
						case BackgroundFillType.BlurredFill:
						default:
							var container = new Border
							{
								Child = new Rectangle
								{
									Stretch = Stretch.Fill,
									Fill = new ImageBrush(originalImage)
									{
										Stretch = Stretch.UniformToFill,
										AlignmentX = AlignmentX.Center,
										AlignmentY = AlignmentY.Center,
									},
									Effect = new BlurEffect()
									{
										KernelType = KernelType.Gaussian,
										Radius = Math.Min(hCount, vCount) * 5,
									}
								},
								Background = new SolidColorBrush(Colors.Black),
							};
							container.Measure(bounds.Size);
							container.Arrange(bounds);
							var blurred = new RenderTargetBitmap(width, height, originalImage.DpiX, originalImage.DpiY, PixelFormats.Pbgra32);
							blurred.Render(container);
							backgroundBrush = new ImageBrush(blurred) { Stretch = Stretch.None, TileMode = TileMode.None };
							break;
					}
					context.DrawRectangle(backgroundBrush, null, bounds);
				}

				// Draw image
				context.DrawImage(originalImage, imgRect);

				// Finalize render
				context.Pop();
				context.Close();
				RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
				bmp.Render(visual);
				PuzzleImage = bmp;

				// Generate pieces
				var pieces = new List<Piece>();
				for (int y = 0; y < vCount; y++)
				{
					for (int x = 0; x < hCount; x++)
					{
						// Determine connections
						ConnectionType north = ConnectionType.Edge;
						ConnectionType south = ConnectionType.Edge;
						ConnectionType east = ConnectionType.Edge;
						ConnectionType west = ConnectionType.Edge;
						if (x < hCount - 1) east = rng.NextDouble() > 0.5 ? ConnectionType.Tab : ConnectionType.Blank;
						if (y < vCount - 1) south = rng.NextDouble() > 0.5 ? ConnectionType.Tab : ConnectionType.Blank;
						if (y > 0) north = (ConnectionType)(-1 * (int)pieces[(y - 1) * hCount + x].SouthConnection);
						if (x > 0) west = (ConnectionType)(-1 * (int)pieces[y * hCount + x - 1].EastConnection);

						// Build and add piece
						var pc = new Piece(x, y, north, south, east, west);
						pieces.Add(pc);
					}
				}
				Pieces = pieces;

				// Close flyout and update VM state
				CommandManager.InvalidateRequerySuggested();
				IsCreatePuzzleFlyoutOpen = false;

				// Reset and begin timer
				ResetTimer();
			}
		}

		private void OnSolvePuzzle()
		{
			SolvePuzzle?.Invoke(this, EventArgs.Empty);
		}

		private void OnRandomizePuzzle()
		{
			RandomizePuzzle?.Invoke(this, EventArgs.Empty);
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			UpdateTimer();
		}

		private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Settings.ShowTimer))
			{
				_Timer.IsEnabled = Settings.Default.ShowTimer;
			}
		}

		#endregion

		#region Timer

		private void ResetTimer()
		{
			_TimerStart = DateTime.Now;
			if (Settings.Default.ShowTimer) _Timer.Start();
		}

		private void UpdateTimer()
		{
			if (_TimerStart.HasValue) Timer = DateTime.Now - _TimerStart.Value;
			else Timer = TimeSpan.Zero;
		}

		#endregion
	}
}
