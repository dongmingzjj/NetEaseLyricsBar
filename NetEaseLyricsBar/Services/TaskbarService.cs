using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace NetEaseLyricsBar.Services
{
    public class TaskbarService
    {
        private static readonly string LogFile = Path.Combine(Path.GetTempPath(), "netease_lyrics_debug.txt");

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, IntPtr lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_VISIBLE = 0x10000000;

        #endregion

        public static TaskbarInfo GetTaskbarInfo()
        {
            var taskbarHwnd = FindWindow("Shell_TrayWnd", IntPtr.Zero);

            if (taskbarHwnd == IntPtr.Zero)
            {
                // 如果找不到任务栏，返回默认值（底部）
                return GetDefaultTaskbarInfo();
            }

            GetWindowRect(taskbarHwnd, out RECT rect);
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // 判断任务栏位置
            string position = DetermineTaskbarPosition(rect, screenWidth, screenHeight);

            return new TaskbarInfo
            {
                Position = position,
                Rect = new RectInt(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight
            };
        }

        private static string DetermineTaskbarPosition(RECT rect, int screenWidth, int screenHeight)
        {
            // 判断任务栏在哪一边
            int threshold = 10; // 容差像素

            if (rect.Bottom >= screenHeight - threshold)
            {
                return "bottom";
            }
            else if (rect.Top <= threshold)
            {
                return "top";
            }
            else if (rect.Left <= threshold)
            {
                return "left";
            }
            else if (rect.Right >= screenWidth - threshold)
            {
                return "right";
            }
            else
            {
                return "bottom"; // 默认
            }
        }

        private static TaskbarInfo GetDefaultTaskbarInfo()
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            int taskbarHeight = 40; // 默认任务栏高度

            return new TaskbarInfo
            {
                Position = "bottom",
                Rect = new RectInt(0, screenHeight - taskbarHeight, screenWidth, taskbarHeight),
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight
            };
        }

        // 获取任务栏上的可用空位
        public static AvailableSpace GetAvailableTaskbarSpace()
        {
            var taskbarHwnd = FindWindow("Shell_TrayWnd", IntPtr.Zero);
            if (taskbarHwnd == IntPtr.Zero)
            {
                Log("未找到任务栏窗口");
                return GetDefaultAvailableSpace();
            }

            GetWindowRect(taskbarHwnd, out RECT taskbarRect);
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            string position = DetermineTaskbarPosition(taskbarRect, screenWidth, screenHeight);

            Log($"任务栏位置: {position}, 矩形: {taskbarRect.Left},{taskbarRect.Top}-{taskbarRect.Right},{taskbarRect.Bottom}");

            // 策略1: 尝试使用窗口类名过滤检测空位
            try
            {
                var childRects = CollectTaskbarIconRects(taskbarHwnd, taskbarRect);
                if (childRects.Count > 0)
                {
                    Log($"成功收集到 {childRects.Count} 个图标窗口");
                    var space = FindLargestGap(taskbarRect, childRects, position, screenWidth, screenHeight);
                    if (space != null)
                        return space;
                }
            }
            catch (Exception ex)
            {
                Log($"窗口类名过滤失败: {ex.Message}");
            }

            // 策略2: Windows 11居中检测
            Log("回退到Windows 11居中布局检测");
            return TryWindows11CenteredLayout(taskbarRect, position);
        }

        private static List<RECT> CollectTaskbarIconRects(IntPtr taskbarHwnd, RECT taskbarRect)
        {
            var iconRects = new List<RECT>();

            EnumChildWindows(taskbarHwnd, (hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                GetWindowRect(hWnd, out RECT rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                // 获取窗口类名
                var className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                string classNameStr = className.ToString();

                Log($"子窗口: 类名={classNameStr}, 矩形={rect.Left},{rect.Top}-{rect.Right},{rect.Bottom} ({width}x{height})");

                // 过滤容器窗口
                if (IsContainerWindow(classNameStr))
                {
                    Log($"  → 过滤(容器窗口)");
                    return true;
                }

                // 只保留图标窗口
                if (IsIconWindow(hWnd, rect, classNameStr))
                {
                    iconRects.Add(rect);
                    Log($"  → 保留(图标窗口)");
                }

                return true;
            }, IntPtr.Zero);

            return iconRects;
        }

        private static bool IsContainerWindow(string className)
        {
            var containerClasses = new[] { "ReBarWindow32", "WorkerW", "SHELLDLL_DefView", "DesktopWorkerW" };
            return containerClasses.Any(c => className.Contains(c));
        }

        private static bool IsIconWindow(IntPtr hWnd, RECT rect, string className)
        {
            // 尺寸过滤
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width < 20 || height < 10 || width > 300 || height > 100)
                return false;

            // 类名过滤 - 保留任务栏相关类名
            var iconClasses = new[] { "ToolbarWindow32", "Button", "MSTaskSwWClass", "TrayNotifyWnd" };
            return iconClasses.Any(c => className.Contains(c));
        }

        private static AvailableSpace TryWindows11CenteredLayout(RECT taskbarRect, string position)
        {
            const int windowWidth = 600;
            const int marginFromEdge = 50;

            int taskbarHeight = taskbarRect.Bottom - taskbarRect.Top;

            return new AvailableSpace
            {
                X = taskbarRect.Left + marginFromEdge,
                Y = taskbarRect.Top,
                Width = windowWidth,
                Height = taskbarHeight,
                Position = position
            };
        }

        private static AvailableSpace GetCenterPosition(RECT taskbarRect, string position, int screenWidth, int screenHeight)
        {
            const int defaultWidth = 600;  // 默认窗口宽度
            const int defaultHeight = 40;  // 默认窗口高度

            int taskbarWidth = taskbarRect.Right - taskbarRect.Left;
            int taskbarHeight = taskbarRect.Bottom - taskbarRect.Top;

            switch (position)
            {
                case "bottom":
                case "top":
                    // 水平任务栏,水平居中
                    int x = taskbarRect.Left + (taskbarWidth - defaultWidth) / 2;
                    int y = taskbarRect.Top;

                    Log($"水平居中位置: X={x}, Y={y}, 宽度={defaultWidth}, 高度={taskbarHeight}");

                    return new AvailableSpace
                    {
                        X = x,
                        Y = y,
                        Width = defaultWidth,
                        Height = taskbarHeight,
                        Position = position
                    };

                case "left":
                    // 左侧任务栏,垂直居中
                    return new AvailableSpace
                    {
                        X = taskbarRect.Left,
                        Y = taskbarRect.Top + (taskbarHeight - defaultHeight) / 2,
                        Width = taskbarWidth,
                        Height = defaultHeight,
                        Position = position
                    };

                case "right":
                    // 右侧任务栏,垂直居中
                    return new AvailableSpace
                    {
                        X = taskbarRect.Left,
                        Y = taskbarRect.Top + (taskbarHeight - defaultHeight) / 2,
                        Width = taskbarWidth,
                        Height = defaultHeight,
                        Position = position
                    };

                default:
                    return GetDefaultAvailableSpace();
            }
        }

        private static AvailableSpace FindLargestGap(RECT taskbarRect, List<RECT> childRects, string position, int screenWidth, int screenHeight)
        {
            const int minGapSize = 200; // 最小空位宽度
            const int margin = 10; // 边缘留白

            Log($"寻找空位, 位置: {position}");

            switch (position)
            {
                case "bottom":
                case "top":
                    // 水平任务栏，找水平方向的空位
                    var gaps = new List<(int start, int width)>();
                    var sortedRects = childRects
                        .Where(r => r.Right - r.Left > 20) // 过滤太小的元素
                        .OrderBy(r => r.Left)
                        .ToList();

                    int lastEnd = taskbarRect.Left + margin;

                    foreach (var rect in sortedRects)
                    {
                        if (rect.Left > lastEnd)
                        {
                            int gapWidth = rect.Left - lastEnd;
                            gaps.Add((lastEnd, gapWidth));
                            Log($"找到空位: {lastEnd} -> {rect.Left}, 宽度: {gapWidth}");
                        }
                        lastEnd = Math.Max(lastEnd, rect.Right);
                    }

                    // 最后一段到任务栏右边缘
                    if (taskbarRect.Right - margin > lastEnd)
                    {
                        int gapWidth = taskbarRect.Right - margin - lastEnd;
                        gaps.Add((lastEnd, gapWidth));
                        Log($"末端空位: {lastEnd} -> {taskbarRect.Right - margin}, 宽度: {gapWidth}");
                    }

                    Log($"总共找到 {gaps.Count} 个空位");

                    // 找最大的空位，优先居中的空位
                    var bestGap = gaps
                        .Where(g => g.width >= minGapSize)
                        .OrderByDescending(g => g.width)
                        .FirstOrDefault();

                    if (bestGap.width > 0)
                    {
                        Log($"选择空位: X={bestGap.start}, 宽度={bestGap.width}");
                        return new AvailableSpace
                        {
                            X = bestGap.start,
                            Y = taskbarRect.Top,
                            Width = bestGap.width,
                            Height = taskbarRect.Bottom - taskbarRect.Top,
                            Position = position
                        };
                    }
                    else
                    {
                        Log($"没有找到大于 {minGapSize}px 的空位,使用默认位置");
                    }
                    break;

                case "left":
                case "right":
                    // 垂直任务栏，找垂直方向的空位
                    var vGaps = new List<(int start, int height)>();
                    var sortedVRects = childRects
                        .Where(r => r.Bottom - r.Top > 20)
                        .OrderBy(r => r.Top)
                        .ToList();

                    int lastVEnd = taskbarRect.Top + margin;

                    foreach (var rect in sortedVRects)
                    {
                        if (rect.Top > lastVEnd)
                        {
                            vGaps.Add((lastVEnd, rect.Top - lastVEnd));
                        }
                        lastVEnd = Math.Max(lastVEnd, rect.Bottom);
                    }

                    if (taskbarRect.Bottom - margin > lastVEnd)
                    {
                        vGaps.Add((lastVEnd, taskbarRect.Bottom - margin - lastVEnd));
                    }

                    var bestVGap = vGaps
                        .Where(g => g.height >= minGapSize)
                        .OrderByDescending(g => g.height)
                        .FirstOrDefault();

                    if (bestVGap.height > 0)
                    {
                        return new AvailableSpace
                        {
                            X = taskbarRect.Left,
                            Y = bestVGap.start,
                            Width = taskbarRect.Right - taskbarRect.Left,
                            Height = bestVGap.height,
                            Position = position
                        };
                    }
                    break;
            }

            // 如果没找到合适的空位，返回默认（居中但宽度较小）
            Log($"使用默认位置");
            return GetDefaultAvailableSpace();
        }

        private static AvailableSpace GetDefaultAvailableSpace()
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            int defaultWidth = Math.Min(600, screenWidth - 100); // 默认宽度，最大600
            int defaultHeight = 40;

            return new AvailableSpace
            {
                X = 50,  // 距左边缘50px，便于用户手动调整
                Y = screenHeight - defaultHeight,
                Width = defaultWidth,
                Height = defaultHeight,
                Position = "bottom"
            };
        }
    }

    public class TaskbarInfo
    {
        public string Position { get; set; } = "bottom";
        public RectInt Rect { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }

        // 获取任务栏上方的坐标
        public double GetPositionAboveTaskbar(double windowHeight)
        {
            switch (Position)
            {
                case "bottom":
                    return ScreenHeight - Rect.Height - windowHeight;
                case "top":
                    return (double)Rect.Height;
                case "left":
                    return (double)Rect.Width;
                case "right":
                    return ScreenWidth - Rect.Width - 600; // 假设窗口宽度600
                default:
                    return ScreenHeight - Rect.Height - windowHeight;
            }
        }

        // 获取水平居中位置
        public double GetCenterX(double windowWidth)
        {
            if (Position == "left" || Position == "right")
            {
                return (ScreenWidth - windowWidth) / 2;
            }
            else
            {
                return (Rect.Width - windowWidth) / 2;
            }
        }
    }

    public struct RectInt
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public RectInt(int left, int top, int width, int height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }

    // 任务栏可用空位信息
    public class AvailableSpace
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Position { get; set; } = "bottom";
    }
}
