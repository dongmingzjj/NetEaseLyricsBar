using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace NetEaseLyricsBar.Services
{
    public class BackgroundColorService
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        /// <summary>
        /// 检测窗口背景颜色并返回合适的文字颜色
        /// </summary>
        public static Color GetOptimalTextColor(IntPtr windowHandle, double width, double height)
        {
            try
            {
                GetWindowRect(windowHandle, out RECT rect);
                int windowWidth = rect.Right - rect.Left;
                int windowHeight = rect.Bottom - rect.Top;

                // 采样窗口中心区域的多个点
                int sampleCount = 9; // 3x3 采样网格
                double brightnessSum = 0;

                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                    return Colors.White;

                try
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int x = rect.Left + (int)(windowWidth * ((i % 3 + 1) / 4.0));
                        int y = rect.Top + (int)(windowHeight * ((i / 3 + 1) / 4.0));

                        uint pixel = GetPixel(hdc, x, y);
                        byte r = (byte)(pixel & 0xFF);
                        byte g = (byte)((pixel >> 8) & 0xFF);
                        byte b = (byte)((pixel >> 16) & 0xFF);

                        // 计算亮度 (使用感知亮度公式)
                        double brightness = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
                        brightnessSum += brightness;
                    }
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }

                double avgBrightness = brightnessSum / sampleCount;

                // 如果背景亮(>0.5),使用深色文字;否则使用浅色文字
                if (avgBrightness > 0.5)
                {
                    return Color.FromRgb(30, 30, 30); // 深色文字
                }
                else
                {
                    return Color.FromRgb(250, 250, 250); // 浅色文字
                }
            }
            catch
            {
                return Colors.White; // 默认白色
            }
        }

        /// <summary>
        /// 获取背景颜色对应的毛玻璃背景色
        /// </summary>
        public static Color GetAcrylicBackgroundColor(Color textColor)
        {
            if (textColor.R < 128) // 深色文字
            {
                // 使用浅色半透明背景
                return Color.FromArgb(180, 255, 255, 255); // 70% 不透明的白色
            }
            else
            {
                // 使用深色半透明背景
                return Color.FromArgb(180, 0, 0, 0); // 70% 不透明的黑色
            }
        }

        /// <summary>
        /// 根据文字颜色获取次要文字颜色(用于歌手名等次要信息)
        /// </summary>
        public static Color GetSecondaryTextColor(Color primaryColor)
        {
            if (primaryColor.R < 128) // 深色文字
            {
                return Color.FromRgb(80, 80, 80); // 中灰色
            }
            else
            {
                return Color.FromRgb(200, 200, 200); // 浅灰色
            }
        }
    }
}
