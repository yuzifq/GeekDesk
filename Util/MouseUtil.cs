using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace GeekDesk.Util
{
    class MouseUtil
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        /// <summary>
        /// 获取鼠标坐标
        /// </summary>
        /// <returns></returns>
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        /// <summary>
        /// 获取鼠标在窗口内的坐标，统一 Win32 屏幕像素与 WPF 逻辑单位。
        /// </summary>
        public static Point GetMousePositionInWindow(Window window)
        {
            Point logicalScreenPosition = GetMouseLogicalScreenPosition(window);
            return new Point(logicalScreenPosition.X - window.Left, logicalScreenPosition.Y - window.Top);
        }

        public static Point GetMouseLogicalScreenPosition(Window window)
        {
            return ScreenToLogical(window, GetMousePosition());
        }

        public static Rect GetVirtualScreenBounds(Window window)
        {
            double screenLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            double screenTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            double screenRight = screenLeft + GetSystemMetrics(SM_CXVIRTUALSCREEN);
            double screenBottom = screenTop + GetSystemMetrics(SM_CYVIRTUALSCREEN);

            Point logicalTopLeft = ScreenToLogical(window, new Point(screenLeft, screenTop));
            Point logicalBottomRight = ScreenToLogical(window, new Point(screenRight, screenBottom));
            return new Rect(logicalTopLeft, logicalBottomRight);
        }

        private static Point ScreenToLogical(Window window, Point screenPosition)
        {
            return GetTransformFromDevice(window).Transform(screenPosition);
        }

        private static Matrix GetTransformFromDevice(Window window)
        {
            PresentationSource source = PresentationSource.FromVisual(window);

            if (source?.CompositionTarget != null)
            {
                return source.CompositionTarget.TransformFromDevice;
            }

            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                System.Windows.Interop.HwndSource hwndSource = System.Windows.Interop.HwndSource.FromHwnd(handle);
                if (hwndSource?.CompositionTarget != null)
                {
                    return hwndSource.CompositionTarget.TransformFromDevice;
                }
            }

            DpiScale dpi = VisualTreeHelper.GetDpi(window);
            return new Matrix(1d / dpi.DpiScaleX, 0, 0, 1d / dpi.DpiScaleY, 0, 0);
        }

        public static bool IsMouseInWindow(Window window)
        {
            if (window == null)
            {
                return false;
            }

            Point mousePosition = GetMousePositionInWindow(window);
            double windowWidth = GetWindowLength(window.ActualWidth, window.Width);
            double windowHeight = GetWindowLength(window.ActualHeight, window.Height);

            return mousePosition.X >= 0
                && mousePosition.X <= windowWidth
                && mousePosition.Y >= 0
                && mousePosition.Y <= windowHeight;
        }

        private static double GetWindowLength(double actualLength, double configuredLength)
        {
            if (!double.IsNaN(actualLength) && actualLength > 0)
            {
                return actualLength;
            }

            if (!double.IsNaN(configuredLength) && configuredLength > 0)
            {
                return configuredLength;
            }

            return 0;
        }


        public static Point GetMousePosition(Visual relativeTo)
        {
            Win32Point mouse = new Win32Point();
            GetCursorPos(ref mouse);

            // Using PointFromScreen instead of Dan Crevier's code (commented out below)
            // is a bug fix created by William J. Roberts.  Read his comments about the fix
            // here: http://www.codeproject.com/useritems/ListViewDragDropManager.asp?msg=1911611#xx1911611xx
            return relativeTo.PointFromScreen(new Point((double)mouse.X, (double)mouse.Y));

            #region Commented Out
            //System.Windows.Interop.HwndSource presentationSource =
            //    (System.Windows.Interop.HwndSource)PresentationSource.FromVisual( relativeTo );
            //ScreenToClient( presentationSource.Handle, ref mouse );
            //GeneralTransform transform = relativeTo.TransformToAncestor( presentationSource.RootVisual );
            //Point offset = transform.Transform( new Point( 0, 0 ) );
            //return new Point( mouse.X - offset.X, mouse.Y - offset.Y );
            #endregion // Commented Out
        }

    }
}
