using System;

namespace Puzzler.ViewModels
{
	public interface IPuzzleController
	{
		event EventHandler SolvePuzzle;
		event EventHandler RandomizePuzzle;

		void OnPuzzleRandomized();
		void OnPuzzleCompleted(bool autoSolved, int moveCount);
	}
}
