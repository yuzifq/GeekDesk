using GeekDesk.Constant;
using GeekDesk.MyThread;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GeekDesk.Util
{

    enum HideType
    {
        NONE = 0,
        TOP_SHOW = 1,
        LEFT_SHOW = 2,
        RIGHT_SHOW = 3,
        TOP_HIDE = 4,
        LEFT_HIDE = 5,
        RIGHT_HIDE = 6
    }


    public class MarginHide
    {
        private static Window window;//定义使用该方法的窗体

        private static readonly int hideTime = 65;
        private static readonly int showTime = 15;

        private static int animalTime;

        private static readonly int fadeHideTime = 50;
        private static readonly int fadeShowTime = 50;
        private static readonly int taskTime = 200;

        public static readonly int shadowWidth = 20;

        public static bool ON_HIDE = false;


        private static double showMarginWidth = 1;
        private static double showTriggerWidth = 4;
        private static double edgePositionTolerance = 2;

        public static bool IS_HIDE = false;

        private static System.Windows.Forms.Timer timer = null;
        private static HideType lastHiddenShowType = HideType.NONE;

        public static void ReadyHide(Window window)
        {
            MarginHide.window = window;
        }


        /// <summary>
        /// 窗体是否贴边
        /// </summary>
        /// <returns></returns>
        public static bool IsMargin()
        {
            Rect screenBounds = MouseUtil.GetVirtualScreenBounds(window);
            double screenLeft = screenBounds.Left;
            double screenTop = screenBounds.Top;
            double screenRight = screenBounds.Right;

            double windowWidth = window.Width;

            double windowTop = window.Top;
            double windowLeft = window.Left;

            //窗体是否贴边
            return (windowLeft <= screenLeft
                || windowTop <= screenTop
                || windowLeft + windowWidth >= screenRight);
        }



        #region 窗体贴边隐藏功能
        private static void HideWindow(object o, EventArgs e)
        {
            try
            {
                if (window == null) return;

                if (RunTimeStatus.MARGIN_HIDE_AND_OTHER_SHOW
                    || RunTimeStatus.LOCK_APP_PANEL) return;

                Rect screenBounds = MouseUtil.GetVirtualScreenBounds(window);
                double screenLeft = screenBounds.Left;
                double screenTop = screenBounds.Top;
                double screenRight = screenBounds.Right;

                double windowHeight = GetWindowHeight();
                double windowWidth = GetWindowWidth();

                double windowTop = window.Top;
                double windowLeft = window.Left;

                bool mouseInWindow = MouseUtil.IsMouseInWindow(window);
                HideType hiddenShowType = GetCurrentHiddenShowType(screenLeft, screenTop, screenRight, windowLeft, windowTop);
                bool mouseOnHiddenEdge = IsMouseOnHiddenEdge(screenLeft, screenTop, screenRight, windowLeft, windowTop, windowWidth, windowHeight, hiddenShowType);

                //鼠标不在窗口上
                if (!mouseInWindow && !IS_HIDE && window.Visibility == Visibility.Visible)
                {
                    //上方隐藏条件
                    if (windowTop <= screenTop)
                    {
                        IS_HIDE = true;
                        lastHiddenShowType = HideType.TOP_SHOW;
                        //FadeAnimation(1, 0);
                        HideAnimation(windowTop, screenTop - windowHeight + showMarginWidth, Window.TopProperty, HideType.TOP_HIDE);
                        return;
                    }
                    //左侧隐藏条件
                    if (windowLeft <= screenLeft)
                    {
                        IS_HIDE = true;
                        lastHiddenShowType = HideType.LEFT_SHOW;
                        //FadeAnimation(1, 0);
                        HideAnimation(windowLeft, screenLeft - windowWidth + showMarginWidth, Window.LeftProperty, HideType.LEFT_HIDE);
                        return;
                    }
                    //右侧隐藏条件
                    if (windowLeft + windowWidth >= screenRight)
                    {
                        IS_HIDE = true;
                        lastHiddenShowType = HideType.RIGHT_SHOW;
                        //FadeAnimation(1, 0);
                        HideAnimation(windowLeft, screenRight - showMarginWidth, Window.LeftProperty, HideType.RIGHT_HIDE);
                        return;
                    }
                }
                else if (mouseOnHiddenEdge && IS_HIDE && window.Visibility != Visibility.Visible)
                {
                    window.Show();
                    IS_HIDE = false;
                    ClearHiddenState();
                    switch (hiddenShowType)
                    {
                        case HideType.TOP_SHOW:
                            HideAnimation(windowTop, screenTop, Window.TopProperty, HideType.TOP_SHOW);
                            return;
                        case HideType.LEFT_SHOW:
                            HideAnimation(windowLeft, screenLeft, Window.LeftProperty, HideType.LEFT_SHOW);
                            return;
                        case HideType.RIGHT_SHOW:
                            HideAnimation(windowLeft, screenRight - windowWidth, Window.LeftProperty, HideType.RIGHT_SHOW);
                            return;
                        default:
                            IS_HIDE = true;
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "贴边隐藏处理异常!");
            }

        }
        #endregion

        private static HideType GetHiddenShowType(double screenLeft, double screenTop, double screenRight, double windowLeft, double windowTop)
        {
            if (windowLeft <= screenLeft - showMarginWidth)
            {
                return HideType.LEFT_SHOW;
            }

            if (windowTop <= screenTop - showMarginWidth)
            {
                return HideType.TOP_SHOW;
            }

            if (Math.Abs(windowLeft - (screenRight - showMarginWidth)) <= edgePositionTolerance)
            {
                return HideType.RIGHT_SHOW;
            }

            return HideType.NONE;
        }

        private static HideType GetCurrentHiddenShowType(double screenLeft, double screenTop, double screenRight, double windowLeft, double windowTop)
        {
            HideType hiddenShowType = GetHiddenShowType(screenLeft, screenTop, screenRight, windowLeft, windowTop);
            if (hiddenShowType != HideType.NONE)
            {
                return hiddenShowType;
            }

            return lastHiddenShowType;
        }

        private static bool IsMouseOnHiddenEdge(double screenLeft, double screenTop, double screenRight, double windowLeft, double windowTop, double windowWidth, double windowHeight, HideType hiddenShowType)
        {
            if (!IS_HIDE || window.Visibility == Visibility.Visible)
            {
                return false;
            }

            Point mousePosition = MouseUtil.GetMouseLogicalScreenPosition(window);
            bool mouseInWindowHeight = mousePosition.Y >= windowTop && mousePosition.Y <= windowTop + windowHeight;
            bool mouseInWindowWidth = mousePosition.X >= windowLeft && mousePosition.X <= windowLeft + windowWidth;

            switch (hiddenShowType)
            {
                case HideType.LEFT_SHOW:
                    return mousePosition.X <= screenLeft + showTriggerWidth && mouseInWindowHeight;
                case HideType.TOP_SHOW:
                    return mousePosition.Y <= screenTop + showTriggerWidth && mouseInWindowWidth;
                case HideType.RIGHT_SHOW:
                    return mousePosition.X >= screenRight - showTriggerWidth && mouseInWindowHeight;
                default:
                    return false;
            }
        }

        private static void ClearHiddenState()
        {
            lastHiddenShowType = HideType.NONE;
        }

        public static void CancelHiddenState()
        {
            IS_HIDE = false;
            ClearHiddenState();
        }

        public static void ShowHiddenWindowForDisplayChange()
        {
            try
            {
                if (window == null || !IS_HIDE)
                {
                    return;
                }

                Rect screenBounds = MouseUtil.GetVirtualScreenBounds(window);
                double windowWidth = GetWindowWidth();
                double windowHeight = GetWindowHeight();
                HideType hiddenShowType = ResolveHiddenShowType(screenBounds, windowWidth, windowHeight);

                RestoreHiddenWindowToVisibleBounds(screenBounds, hiddenShowType, windowWidth, windowHeight);
                IS_HIDE = false;
                ClearHiddenState();
                RunTimeStatus.MARGIN_HIDE_AND_OTHER_SHOW = false;
                window.Opacity = 1;
                window.Visibility = Visibility.Visible;
                WindowUtil.SendToBottomNoActivate(window);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "显示设置变化后恢复贴边隐藏窗口异常!");
            }
        }

        private static void RestoreHiddenWindowToVisibleBounds(Rect screenBounds, HideType hiddenShowType, double windowWidth, double windowHeight)
        {
            switch (hiddenShowType)
            {
                case HideType.LEFT_SHOW:
                    window.Left = screenBounds.Left;
                    window.Top = Clamp(GetValidCoordinate(window.Top, screenBounds.Top), screenBounds.Top, screenBounds.Bottom - windowHeight);
                    break;
                case HideType.TOP_SHOW:
                    window.Left = Clamp(GetValidCoordinate(window.Left, screenBounds.Left), screenBounds.Left, screenBounds.Right - windowWidth);
                    window.Top = screenBounds.Top;
                    break;
                case HideType.RIGHT_SHOW:
                    window.Left = screenBounds.Right - windowWidth;
                    window.Top = Clamp(GetValidCoordinate(window.Top, screenBounds.Top), screenBounds.Top, screenBounds.Bottom - windowHeight);
                    break;
                default:
                    window.Left = Clamp(GetValidCoordinate(window.Left, screenBounds.Left), screenBounds.Left, screenBounds.Right - windowWidth);
                    window.Top = Clamp(GetValidCoordinate(window.Top, screenBounds.Top), screenBounds.Top, screenBounds.Bottom - windowHeight);
                    break;
            }
        }

        private static HideType ResolveHiddenShowType(Rect screenBounds, double windowWidth, double windowHeight)
        {
            HideType hiddenShowType = GetCurrentHiddenShowType(screenBounds.Left, screenBounds.Top, screenBounds.Right, window.Left, window.Top);
            if (hiddenShowType != HideType.NONE)
            {
                return hiddenShowType;
            }

            double windowLeft = GetValidCoordinate(window.Left, screenBounds.Right - showMarginWidth);
            double windowTop = GetValidCoordinate(window.Top, screenBounds.Top);
            double leftHiddenPosition = screenBounds.Left - windowWidth + showMarginWidth;
            double topHiddenPosition = screenBounds.Top - windowHeight + showMarginWidth;
            double rightHiddenPosition = screenBounds.Right - showMarginWidth;

            double leftDistance = Math.Abs(windowLeft - leftHiddenPosition);
            double topDistance = Math.Abs(windowTop - topHiddenPosition);
            double rightDistance = Math.Abs(windowLeft - rightHiddenPosition);

            if (topDistance <= leftDistance && topDistance <= rightDistance)
            {
                return HideType.TOP_SHOW;
            }

            return leftDistance <= rightDistance ? HideType.LEFT_SHOW : HideType.RIGHT_SHOW;
        }

        private static bool IsValidCoordinate(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double GetValidCoordinate(double value, double fallback)
        {
            return IsValidCoordinate(value) ? value : fallback;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (max < min)
            {
                return min;
            }

            return Math.Max(min, Math.Min(max, value));
        }


        public static void StartHide()
        {
            ON_HIDE = true;
            if (timer != null) return;
            timer = new System.Windows.Forms.Timer
            {
                Interval = taskTime
            };//添加timer计时器，隐藏功能
            timer.Tick += HideWindow;
            timer.Start();
        }

        public static void StopHide()
        {
            ON_HIDE = false;
            if (timer == null) return;
            timer.Stop();
            timer.Dispose();
            timer = null;
            //功能关闭 如果界面是隐藏状态  那么要显示界面 ↓
            if (IS_HIDE)
            {
                Rect screenBounds = MouseUtil.GetVirtualScreenBounds(window);
                double screenLeft = screenBounds.Left;
                double screenTop = screenBounds.Top;
                double screenRight = screenBounds.Right;

                double windowWidth = GetWindowWidth();

                double windowTop = window.Top;
                double windowLeft = window.Left;
                HideType hiddenShowType = ResolveHiddenShowType(screenBounds, windowWidth, GetWindowHeight());

                window.Visibility = Visibility.Visible;
                //左侧显示
                if (hiddenShowType == HideType.LEFT_SHOW)
                {
                    IS_HIDE = false;
                    ClearHiddenState();
                    //FadeAnimation(0, 1);
                    HideAnimation(windowLeft, screenLeft, Window.LeftProperty, HideType.LEFT_SHOW);
                    return;
                }

                //上方显示
                if (hiddenShowType == HideType.TOP_SHOW)
                {
                    IS_HIDE = false;
                    ClearHiddenState();
                    //FadeAnimation(0, 1);
                    HideAnimation(windowTop, screenTop, Window.TopProperty, HideType.TOP_SHOW);
                    return;
                }

                //右侧显示
                if (hiddenShowType == HideType.RIGHT_SHOW)
                {
                    IS_HIDE = false;
                    ClearHiddenState();
                    //FadeAnimation(0, 1);
                    HideAnimation(windowLeft, screenRight - windowWidth, Window.LeftProperty, HideType.RIGHT_SHOW);
                    return;
                }

                IS_HIDE = false;
                ClearHiddenState();
            }
        }

        private static double GetWindowWidth()
        {
            if (!double.IsNaN(window.ActualWidth) && window.ActualWidth > 0)
            {
                return window.ActualWidth;
            }

            return !double.IsNaN(window.Width) && window.Width > 0 ? window.Width : 0;
        }

        private static double GetWindowHeight()
        {
            if (!double.IsNaN(window.ActualHeight) && window.ActualHeight > 0)
            {
                return window.ActualHeight;
            }

            return !double.IsNaN(window.Height) && window.Height > 0 ? window.Height : 0;
        }


        private static void HideAnimation(double from, double to, DependencyProperty property, HideType hideType)
        {

            new Thread(() =>
            {
                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {

                        switch (hideType)
                        {
                            case HideType.LEFT_SHOW:
                                to -= 20;
                                break;
                            case HideType.RIGHT_SHOW:
                                to += 20;
                                break;
                            case HideType.TOP_SHOW:
                                to -= 20;
                                break;
                        }

                        double abs = Math.Abs(Math.Abs(to) - Math.Abs(from));

                        if (hideType <= HideType.RIGHT_SHOW)
                        {
                            animalTime = showTime;
                        }
                        else
                        {
                            animalTime = hideTime;
                        }
                        double subLen = abs / animalTime;
                        int count = 0;
                        while (count < animalTime)
                        {
                            switch (hideType)
                            {
                                case HideType.LEFT_HIDE:
                                    window.Left -= subLen;
                                    break;
                                case HideType.LEFT_SHOW:
                                    window.Left += subLen;
                                    break;
                                case HideType.RIGHT_HIDE:
                                    window.Left += subLen;
                                    break;
                                case HideType.RIGHT_SHOW:
                                    window.Left -= subLen;
                                    break;
                                case HideType.TOP_HIDE:
                                    window.Top -= subLen;
                                    break;
                                case HideType.TOP_SHOW:
                                    window.Top += subLen;
                                    break;
                            }
                            count++;
                            Thread.Sleep(1);
                        }

                        switch (hideType)
                        {
                            case HideType.TOP_HIDE:
                                window.Top = to;
                                break;
                            case HideType.TOP_SHOW:
                                window.Top = to;
                                break;
                            default:
                                window.Left = to;
                                break;
                        }
                        if (hideType > HideType.RIGHT_SHOW)
                        {
                            window.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ClearHiddenState();
                        }
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogUtil.WriteErrorLog(ex, "贴边隐藏动画异常!");
                    }));
                }
            }).Start();
            
            
        }

        private static void FadeAnimation(double from, double to)
        {
            new Thread(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    double time;
                    if (to == 0D)
                    {
                        time = fadeHideTime;
                    }
                    else
                    {
                        time = fadeShowTime;
                    }
                    DoubleAnimation opacityAnimation = new DoubleAnimation
                    {
                        From = from,
                        To = to,
                        Duration = new Duration(TimeSpan.FromMilliseconds(time))
                    };
                    opacityAnimation.Completed += (s, e) =>
                    {
                        //window.Opacity = to;
                        window.BeginAnimation(Window.OpacityProperty, null);
                    };
                    Timeline.SetDesiredFrameRate(opacityAnimation, 60);
                    window.BeginAnimation(Window.OpacityProperty, opacityAnimation);
                });
            }).Start();
        }


        public static void WaitHide(int waitTime)
        {
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(waitTime);
                //修改状态为false 继续执行贴边隐藏
                RunTimeStatus.MARGIN_HIDE_AND_OTHER_SHOW = false;
            });
            t.IsBackground = true;
            t.Start();
        }





    }
}
