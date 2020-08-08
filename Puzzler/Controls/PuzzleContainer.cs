using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Puzzler.Controls
{
	public class PuzzleContainer : ScrollViewer
	{
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
            var window = Window.GetWindow(this);
            if (window != null)
			{
                var wih = new WindowInteropHelper(window);
                var source = HwndSource.FromHwnd(wih.EnsureHandle());
                if (source != null)
				{
                    source.AddHook(WndProcHook);
				}
			}
        }

		private void HandleMouseHorizontalWheel(IntPtr wParam)
		{
            // Tilt value
            int tilt = -Win32.HiWord(wParam);
            if (tilt == 0) return;

            // Hit-test to see if the mouse is over the current element
            if (InputHitTest(Mouse.GetPosition(this)) == null) return;

            // Handle scrolling
            if (tilt > 0) LineLeft();
            else LineRight();
        }

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32.WM_MOUSEHWHEEL:
                    HandleMouseHorizontalWheel(wParam);
                    break;
            }
            return IntPtr.Zero;
        }

        private static class Win32
        {
            public const int WM_MOUSEHWHEEL = 0x020E;

            public static int GetIntUnchecked(IntPtr value)
            {
                return IntPtr.Size == 8 ? unchecked((int)value.ToInt64()) : value.ToInt32();
            }

            public static int HiWord(IntPtr ptr)
            {
                return unchecked((short)((uint)GetIntUnchecked(ptr) >> 16));
            }
        }
    }
}
