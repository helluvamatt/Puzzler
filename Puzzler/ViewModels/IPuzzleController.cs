using System;

namespace Puzzler.ViewModels
{
	public interface IPuzzleController
	{
		event EventHandler SolvePuzzle;
		event EventHandler RandomizePuzzle;

		void OnPuzzleRandomized(bool wasSolved);
		void OnPuzzleCompleted(bool autoSolved, int moveCount);
	}
}
