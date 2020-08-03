using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Puzzler.Services
{
	/// <summary>
	/// Really simple animation class
	/// </summary>
	public class Animator
	{
		private readonly TimeSpan _Duration;
		private readonly Action _FrameCallback;
		private readonly List<IAnimation> _Animations;

		private DateTime _Start;

		public Animator(TimeSpan duration, Action frameCallback)
		{
			_Duration = duration;
			_FrameCallback = frameCallback ?? throw new ArgumentNullException(nameof(frameCallback));
			_Animations = new List<IAnimation>();
		}

		public event EventHandler Completed;

		public void AddAnimation(IAnimation animation)
		{
			_Animations.Add(animation);
		}

		public void Start()
		{
			_Start = DateTime.Now;
			CompositionTarget.Rendering += OnCompositionTargetRendering;
		}

		private void OnCompositionTargetRendering(object sender, EventArgs e)
		{
			double progress = _Duration.TotalMilliseconds > 0 ? ((DateTime.Now - _Start).TotalMilliseconds / _Duration.TotalMilliseconds) : 1.0;
			if (progress > 1.0) progress = 1.0;
			foreach (var anim in _Animations)
			{
				anim.Animate(progress);
			}
			_FrameCallback.Invoke();
			if (progress >= 1.0)
			{
				CompositionTarget.Rendering -= OnCompositionTargetRendering;
				Completed?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public interface IAnimation
	{
		void Animate(double progress);
	}

	public class PointAnimation : IAnimation
	{
		private readonly Point _From;
		private readonly Point _To;
		private readonly Action<Point> _Callback;

		public PointAnimation(Point from, Point to, Action<Point> callback)
		{
			_From = from;
			_To = to;
			_Callback = callback ?? throw new ArgumentNullException(nameof(callback));
		}

		public void Animate(double progress)
		{
			double x = _From.X + (_To.X - _From.X) * progress;
			double y = _From.Y + (_To.Y - _From.Y) * progress;
			_Callback.Invoke(new Point(x, y));
		}
	}
}
