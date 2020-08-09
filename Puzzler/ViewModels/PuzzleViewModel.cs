using Puzzler.Controls;
using Puzzler.Models;
using Puzzler.Properties;
using Puzzler.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Puzzler.ViewModels
{
	public class PuzzleViewModel : DependencyObject, IDisposable
	{
		private const double SnapDistance = 10.0; // "board" pixels where a piece is 50 pixels wide, actual render pixels are dependent on the Zoom

		private readonly IDialogService _DialogService;
		private readonly int _PuzzleWidth;
		private readonly int _PuzzleHeight;
		private readonly DispatcherTimer _Timer;
		private readonly Random _Random;

		private DateTime? _TimerStart;
		private TimeSpan _PreviousTime;

		public PuzzleViewModel(IDialogService dialogService, IEnumerable<Piece> pieces, BitmapSource image, int puzzleWidth, int puzzleHeight)
		{
			_DialogService = dialogService;
			Pieces = pieces;
			Image = image;
			_PuzzleWidth = puzzleWidth;
			_PuzzleHeight = puzzleHeight;
			_Timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
			{
				Interval = TimeSpan.FromMilliseconds(50),
			};
			_Random = new Random();

			Groups = new List<PuzzlePieceGroup>();
			PieceControls = new List<PuzzlePieceControl>();

			_Timer.Tick += OnTimerTick;
			Settings.Default.PropertyChanged += OnSettingsPropertyChanged;

			MaxX = Pieces.Max(p => p.X);
			MaxY = Pieces.Max(p => p.Y);

			// Based on maxX and maxY, determine the range of possible random cell locations
			double maxXPos = (MaxX + 2) * PuzzlePieceControl.GridSize;
			double maxYPos = (MaxY + 2) * PuzzlePieceControl.GridSize;

			// Build pieces
			Point pt;
			foreach (Piece piece in Pieces)
			{
				pt = GetRandomPoint(maxXPos, maxYPos);
				PieceControls.Add(new PuzzlePieceControl(piece, Image, pt.X, pt.Y));
			}
		}

		public IEnumerable<Piece> Pieces { get; }
		public List<PuzzlePieceGroup> Groups { get; }
		public List<PuzzlePieceControl> PieceControls { get; }
		public BitmapSource Image { get; }
		public int MaxX { get; }
		public int MaxY { get; }

		public bool IsSolved { get; private set; }

		#region Dependency properties

		#region MoveCount

		public static readonly DependencyProperty MoveCountProperty = DependencyProperty.Register(nameof(MoveCount), typeof(int), typeof(PuzzleViewModel), new PropertyMetadata(0));

		public int MoveCount
		{
			get => (int)GetValue(MoveCountProperty);
			set => SetValue(MoveCountProperty, value);
		}

		#endregion

		#region Timer

		public static readonly DependencyProperty TimerProperty = DependencyProperty.Register(nameof(Timer), typeof(TimeSpan), typeof(PuzzleViewModel), new PropertyMetadata(TimeSpan.Zero));

		public TimeSpan Timer
		{
			get => (TimeSpan)GetValue(TimerProperty);
			set => SetValue(TimerProperty, value);
		}

		#endregion

		#endregion

		#region Events (VM -> PuzzleControl)

		public event EventHandler SolvePuzzleRequested;
		public event EventHandler RandomizePuzzleRequested;

		#endregion

		#region Methods (PuzzleControl -> VM)

		public void OnPuzzleRandomized(bool wasSolved)
		{
			if (wasSolved)
			{
				ResetTimer();
				MoveCount = 0;
				IsSolved = false;
			}
		}

		public void OnPuzzleCompleted(bool autoSolved)
		{
			IsSolved = true;

			// Stop timer
			_Timer.Stop();
			UpdateTimer();
			_TimerStart = null;

			// Show completed message and statistics
			if (!autoSolved)
			{
				int moves = MoveCount;
				int pieces = _PuzzleWidth * _PuzzleHeight;
				float avgMovesPerPiece = moves / (float)pieces;

				var statisticsBuilder = new StringBuilder();
				statisticsBuilder.AppendLine($"Pieces Count:       {pieces}");
				statisticsBuilder.AppendLine($"Move Count:         {moves}");
				statisticsBuilder.AppendLine($"Total Time:         {Timer}");
				statisticsBuilder.AppendLine($"Avg. Moves / Piece: {avgMovesPerPiece}");

				_DialogService.ShowMessageBox("Complete!", "You have completed the puzzle!", MessageBoxType.Info, statisticsBuilder.ToString().TrimEnd());
			}
			else
			{
				Groups.Clear();
				Groups.Add(new PuzzlePieceGroup(PieceControls));
				MoveCount = 0;
			}
		}

		public PuzzlePieceGroup FindGroup(PuzzlePieceControl ppc)
		{
			return Groups.FirstOrDefault(g => g.Pieces.Contains(ppc));
		}

		public void BringToTop(PuzzlePieceControl el)
		{
			BringToTop(new PuzzlePieceControl[] { el });
		}

		public void BringToTop(PuzzlePieceGroup group)
		{
			BringToTop(group.Pieces);
		}

		public void OnMove(PuzzlePieceControl movedPiece, PuzzlePieceGroup movedGroup)
		{
			if (!IsSolved)
			{
				MoveCount++;

				// May be called with a null piece, which indicates a random movement of all pieces
				if (movedPiece != null)
				{
					// Check links
					var piecesToCheck = new HashSet<PuzzlePieceControl>();
					if (movedGroup != null)
					{
						foreach (var groupItem in movedGroup.Pieces)
						{
							piecesToCheck.Add(groupItem);
						}
					}
					else
					{
						piecesToCheck.Add(movedPiece);
					}

					foreach (var piece in piecesToCheck)
					{
						if (piece.X > 0)
						{
							// Check piece to left (X - 1)
							var neighbor = FindPiece(piece.X - 1, piece.Y);
							if (SnapCheckNeighborX(piece.Position, neighbor.Position, -PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
						}
						if (piece.Y > 0)
						{
							// Check piece above (Y - 1)
							var neighbor = FindPiece(piece.X, piece.Y - 1);
							if (SnapCheckNeighborY(piece.Position, neighbor.Position, -PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
						}
						if (piece.X < MaxX)
						{
							// Check piece to right (X + 1)
							var neighbor = FindPiece(piece.X + 1, piece.Y);
							if (SnapCheckNeighborX(piece.Position, neighbor.Position, PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
						}
						if (piece.Y < MaxY)
						{
							// Check piece below (Y + 1)
							var neighbor = FindPiece(piece.X, piece.Y + 1);
							if (SnapCheckNeighborY(piece.Position, neighbor.Position, PuzzlePieceControl.GridSize)) MergeGroups(piece, neighbor);
						}
					}

					// Check if the puzzle has now been solved
					if (CheckSolution())
					{
						OnPuzzleCompleted(false);
					}
				}
			}
		}

		public Point GetRandomPoint(double maxX, double maxY)
		{
			double x = _Random.NextDouble() * maxX;
			double y = _Random.NextDouble() * maxY;
			return new Point(x, y);
		}

		public Point GetSolvedPoint(PuzzlePieceControl ppc)
		{
			double x = ppc.X * PuzzlePieceControl.GridSize;
			double y = ppc.Y * PuzzlePieceControl.GridSize;
			return new Point(x, y);
		}

		#endregion

		#region Methods (VM -> PuzzleControl)

		public void Solve()
		{
			SolvePuzzleRequested?.Invoke(this, EventArgs.Empty);
		}

		public void Randomize()
		{
			if (IsSolved)
			{
				// Break all groups
				Groups.Clear();
			}
			else
			{
				// Count randomizing as a move
				OnMove(null, null);
			}
			RandomizePuzzleRequested?.Invoke(this, EventArgs.Empty);
		}

		public void PauseTimer()
		{
			_Timer.Stop();
			_PreviousTime += DateTime.Now - _TimerStart.Value;
		}

		public void ResumeTimer()
		{
			_TimerStart = DateTime.Now;
			if (Settings.Default.ShowTimer) _Timer.Start();
		}

		public void Dispose()
		{
			_Timer.Stop();
			_Timer.Tick -= OnTimerTick;
			Settings.Default.PropertyChanged -= OnSettingsPropertyChanged;
		}

		#endregion

		#region Event handlers

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

		#region Private methods

		private void ResetTimer()
		{
			_TimerStart = DateTime.Now;
			_PreviousTime = TimeSpan.Zero;
			if (Settings.Default.ShowTimer) _Timer.Start();
		}

		private void UpdateTimer()
		{
			if (_TimerStart.HasValue) Timer = _PreviousTime + (DateTime.Now - _TimerStart.Value);
			else Timer = TimeSpan.Zero;
		}

		private bool CheckSolution()
		{
			// There should be a single group...
			if (Groups.Count != 1) return false;

			// ... with all the pieces
			if (PieceControls.Except(Groups[0].Pieces).Any()) return false;

			// Solved!
			return true;
		}

		private void BringToTop(IEnumerable<PuzzlePieceControl> els)
		{
			// Recompute all children ZIndex
			var pieces = new List<PuzzlePieceControl>(PieceControls.Except(els));
			pieces.AddRange(els);
			PieceControls.Clear();
			PieceControls.AddRange(pieces);
		}

		private PuzzlePieceControl FindPiece(int x, int y)
		{
			return PieceControls.FirstOrDefault(ppc => ppc.X == x && ppc.Y == y);
		}

		private bool SnapCheck(double a, double b) => Math.Abs(a - b) <= SnapDistance;
		private bool SnapCheckNeighborX(Point loc, Point neighbor, double dX) => SnapCheck(loc.X + dX, neighbor.X) && SnapCheck(loc.Y, neighbor.Y);
		private bool SnapCheckNeighborY(Point loc, Point neighbor, double dY) => SnapCheck(loc.X, neighbor.X) && SnapCheck(loc.Y + dY, neighbor.Y);

		private void MergeGroups(PuzzlePieceControl movedPiece, PuzzlePieceControl neighbor)
		{
			var movedGroup = FindGroup(movedPiece);
			var neighborGroup = FindGroup(neighbor);
			if (neighborGroup != null)
			{
				if (movedGroup != null)
				{
					MergeGroups(movedGroup, neighborGroup);
				}
				else
				{
					neighborGroup.AddPiece(movedPiece);
				}
			}
			else
			{
				if (movedGroup != null)
				{
					movedGroup.AddPiece(neighbor);
				}
				else
				{
					var g = new PuzzlePieceGroup(neighbor);
					g.AddPiece(movedPiece);
					Groups.Add(g);
				}
			}
		}

		private void MergeGroups(PuzzlePieceGroup group, PuzzlePieceGroup neighborGroup)
		{
			if (group == neighborGroup) return;

			neighborGroup.MergeWith(group);
			Groups.Remove(group);
		}

		#endregion
	}
}
