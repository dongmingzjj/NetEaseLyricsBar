using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NetEaseLyricsBar.Models;

namespace NetEaseLyricsBar.Services
{
    /// <summary>
    /// 网易云音乐窗口监听服务
    /// </summary>
    public class NeteaseMusicMonitor
    {
        // Windows API
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // 网易云音乐窗口类名
        private const string NeteaseClassName = "OrpheusBrowserHost";

        // 当前监听的歌曲信息
        private SongInfo? _currentSong;
        private string? _lastTitle; // 记录上次的窗口标题
        private bool _wasPlaying = false; // 记录上次是否在播放

        /// <summary>
        /// 歌曲信息变更事件
        /// </summary>
        public event EventHandler<SongInfo>? SongChanged;

        /// <summary>
        /// 播放进度变化事件（检测到进度条拖动或暂停）
        /// </summary>
        public event EventHandler<double>? ProgressChanged;

        /// <summary>
        /// 启动监听
        /// </summary>
        /// <param name="intervalMs">检查间隔（毫秒）</param>
        public void Start(int intervalMs = 1000)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var songInfo = GetNeteaseMusicInfo();
                        if (songInfo != null && HasSongChanged(songInfo))
                        {
                            _currentSong = songInfo;
                            System.Diagnostics.Debug.WriteLine($"[事件] SongChanged 触发: {songInfo.FullTitle}");
                            SongChanged?.Invoke(this, songInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[监听] ✗ 异常: {ex.Message}");
                    }

                    Task.Delay(intervalMs).Wait();
                }
            });
        }

        /// <summary>
        /// 检查歌曲是否改变
        /// </summary>
        private bool HasSongChanged(SongInfo newSong)
        {
            if (_currentSong == null)
            {
                // 第一次检测，记录时间
                newSong.StartTime = DateTime.Now;
                return true;
            }

            var changed = _currentSong.Name != newSong.Name || _currentSong.Artist != newSong.Artist;

            if (changed)
            {
                // 歌曲变更，记录新的开始时间
                newSong.StartTime = DateTime.Now;
            }

            return changed;
        }

        /// <summary>
        /// 获取网易云音乐当前播放的歌曲信息
        /// </summary>
        /// <returns>歌曲信息，如果未找到返回null</returns>
        private SongInfo? GetNeteaseMusicInfo()
        {
            System.Diagnostics.Debug.WriteLine("[监听] 正在枚举窗口，寻找网易云...");

            IntPtr neteaseWindow = IntPtr.Zero;
            int windowCount = 0;

            // 查找网易云音乐窗口
            EnumWindows((hWnd, lParam) =>
            {
                windowCount++;
                var className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);

                // 记录所有可能的网易云窗口（调试用）
                if (className.ToString().Contains("Orpheus") ||
                    className.ToString().Contains("Chrome") ||
                    className.ToString().Contains("Netease"))
                {
                    var title = new StringBuilder(256);
                    GetWindowText(hWnd, title, title.Capacity);
                    System.Diagnostics.Debug.WriteLine($"[窗口] 类名={className}, 标题={title}");
                }

                if (className.ToString() == NeteaseClassName)
                {
                    neteaseWindow = hWnd;
                    return false; // 停止枚举
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            if (neteaseWindow == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[监听] ✗ 未找到网易云音乐窗口");
                System.Diagnostics.Debug.WriteLine($"[监听] 共枚举了 {windowCount} 个窗口");
                return null;
            }

            // 获取窗口标题
            var title = new StringBuilder(256);
            GetWindowText(neteaseWindow, title, title.Capacity);
            var titleText = title.ToString();

            System.Diagnostics.Debug.WriteLine($"[监听] ✓ 找到网易云窗口: {titleText}");

            // 检测窗口标题变化（用于检测播放状态变化）
            if (_lastTitle != null && _lastTitle != titleText)
            {
                // 标题变化，可能是播放状态改变
                System.Diagnostics.Debug.WriteLine("[监听] 检测到窗口标题变化");

                // 简单的启发式检测：如果标题突然变短或变长，可能是暂停/播放切换
                bool isPlaying = !titleText.Contains(" - 网易云") &&
                                  !titleText.Contains("网易云音乐");

                if (isPlaying != _wasPlaying)
                {
                    System.Diagnostics.Debug.WriteLine($"[监听] 播放状态变化: {_wasPlaying} -> {isPlaying}");
                    _wasPlaying = isPlaying;

                    // 触发进度重置事件（使用-1表示需要重置）
                    ProgressChanged?.Invoke(this, -1);
                }
            }
            _lastTitle = titleText;

            // 解析标题（格式：歌名 - 歌手）
            var result = ParseWindowTitle(titleText);
            if (result != null)
            {
                System.Diagnostics.Debug.WriteLine($"[监听] ✓ 解析成功: {result.FullTitle}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[监听] ✗ 解析窗口标题失败: {titleText}");
            }

            return result;
        }

        /// <summary>
        /// 解析窗口标题
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <returns>歌曲信息，解析失败返回null</returns>
        private SongInfo? ParseWindowTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            // 过滤掉不需要的标题
            if (title.Contains("网易云音乐") || title.Contains("NeteaseCloud"))
            {
                return null;
            }

            // 格式：歌名 - 歌手
            var parts = title.Split(new[] { " - " }, StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                return new SongInfo
                {
                    Name = parts[0].Trim(),
                    Artist = parts[1].Trim()
                };
            }

            return null;
        }

        /// <summary>
        /// 手动获取当前歌曲信息（用于调试）
        /// </summary>
        /// <returns>当前歌曲信息</returns>
        public SongInfo? GetCurrentSong()
        {
            return _currentSong;
        }
    }
}
