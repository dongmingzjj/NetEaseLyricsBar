using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using NetEaseLyricsBar.Models;
using NetEaseLyricsBar.Services;

namespace NetEaseLyricsBar.ViewModels
{
    /// <summary>
    /// 主视图模型
    /// 简化版：仅在歌曲变更时更新，使用内部计时器
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly LyricsPlayerService _lyricsPlayer;
        private readonly NeteaseMusicMonitor _musicMonitor;
        private readonly NeteaseApiService _apiService;
        private readonly SmartProgressTracker _progressTracker;
        private Lyrics? _currentLyrics;
        private bool _isLoading = false;
        private string _statusMessage = "等待播放音乐...";

        // 歌词同步计时器
        private DispatcherTimer? _lyricsTimer;

        public MainViewModel()
        {
            _lyricsPlayer = new LyricsPlayerService();
            _musicMonitor = new NeteaseMusicMonitor();
            _apiService = new NeteaseApiService();
            _progressTracker = new SmartProgressTracker();

            // 订阅歌词行变更事件
            _lyricsPlayer.LineChanged += OnLyricLineChanged;

            // 订阅歌曲变更事件
            _musicMonitor.SongChanged += OnSongChanged;

            // 启动监听（每2秒检查一次）
            _musicMonitor.Start(2000);

            // 初始化歌词同步计时器
            InitializeLyricsTimer();

            // 初始化为空的歌词
            _currentLyrics = new Lyrics
            {
                Text = "等待播放音乐...",
                Artist = "网易云音乐"
            };

            System.Diagnostics.Debug.WriteLine("[ViewModel] ✓ 初始化完成（简化版：计时器模式）");
        }

        /// <summary>
        /// 初始化歌词同步计时器
        /// </summary>
        private void InitializeLyricsTimer()
        {
            _lyricsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // 每200毫秒更新一次
            };

            _lyricsTimer.Tick += (s, e) =>
            {
                try
                {
                    if (_lyricsPlayer.CurrentLyrics != null && !_lyricsPlayer.CurrentLyrics.IsEmpty)
                    {
                        // 使用内部计时器获取当前进度
                        var currentTime = _progressTracker.GetCurrentProgress();

                        // 更新歌词行
                        _lyricsPlayer.UpdateByTime(currentTime);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ViewModel] 歌词同步错误: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// 启动歌词同步
        /// </summary>
        private void StartLyricsSync()
        {
            _progressTracker.StartTracking(0);
            _lyricsTimer?.Start();
            System.Diagnostics.Debug.WriteLine("[ViewModel] ✓ 歌词同步已启动");
        }

        /// <summary>
        /// 停止歌词同步
        /// </summary>
        private void StopLyricsSync()
        {
            _progressTracker.StopTracking();
            _lyricsTimer?.Stop();
            System.Diagnostics.Debug.WriteLine("[ViewModel] ✓ 歌词同步已停止");
        }

        public Lyrics CurrentLyrics
        {
            get => _currentLyrics;
            set
            {
                _currentLyrics = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 歌曲变更时获取歌词
        /// </summary>
        private async void OnSongChanged(object? sender, SongInfo songInfo)
        {
            var requestStartTime = DateTime.Now; // 记录请求开始时间

            try
            {
                System.Diagnostics.Debug.WriteLine("[ViewModel] ===== 歌曲变更 =====");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] 歌名: {songInfo.Name}");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] 歌手: {songInfo.Artist}");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] 歌曲检测时间: {songInfo.StartTime:HH:mm:ss.fff}");

                StatusMessage = $"正在获取歌词: {songInfo.FullTitle}";
                IsLoading = true;

                // 异步获取歌词
                System.Diagnostics.Debug.WriteLine($"[ViewModel] 正在调用 FetchLyricsAsync...");
                var lyricsData = await _apiService.FetchLyricsAsync(songInfo.Name, songInfo.Artist);

                var loadEndTime = DateTime.Now; // 记录加载完成时间

                // 计算总延迟：
                // 1. 检测延迟：从歌曲开始到我们检测到（最多2秒，平均1秒）
                // 2. 加载延迟：从检测到加载完成
                var detectionLatency = (requestStartTime - songInfo.StartTime).TotalSeconds;
                var loadLatency = (loadEndTime - requestStartTime).TotalSeconds;
                var totalLatency = detectionLatency + loadLatency;

                System.Diagnostics.Debug.WriteLine($"[ViewModel] ⏱ 检测延迟: {detectionLatency*1000:F0}ms");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] ⏱ 加载延迟: {loadLatency*1000:F0}ms");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] ⏱ 总延迟: {totalLatency*1000:F0}ms");

                if (lyricsData != null && !lyricsData.IsEmpty)
                {
                    System.Diagnostics.Debug.WriteLine($"[ViewModel] ✓ 成功获取 {lyricsData.Lines.Count} 行歌词");
                    System.Diagnostics.Debug.WriteLine($"[ViewModel] 第一句: {lyricsData.Lines[0].Text}");

                    _lyricsPlayer.LoadLyrics(lyricsData);
                    StatusMessage = $"正在播放: {songInfo.FullTitle}";

                    // 重置并启动歌词同步，应用总延迟补偿
                    StopLyricsSync();
                    _progressTracker.StartTracking(totalLatency); // 从总延迟开始计时
                    _lyricsTimer?.Start();

                    System.Diagnostics.Debug.WriteLine($"[ViewModel] ✓ 歌词同步已启动（延迟补偿: {totalLatency*1000:F0}ms）");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ViewModel] ✗ 未获取到歌词或歌词为空");

                    // 歌词获取失败，显示歌曲名
                    _lyricsPlayer.LoadLyrics(null);
                    CurrentLyrics = new Lyrics
                    {
                        Text = songInfo.Name,
                        Artist = songInfo.Artist
                    };
                    StatusMessage = $"未找到歌词";

                    // 停止歌词同步
                    StopLyricsSync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] ✗ 获取歌词异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] 异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] 异常详情: {ex.StackTrace}");
                StatusMessage = "获取歌词失败";

                // 显示友好提示
                CurrentLyrics = new Lyrics
                {
                    Text = "歌词获取失败",
                    Artist = "请检查网络连接"
                };

                // 停止歌词同步
                StopLyricsSync();
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine($"[ViewModel] ===== 歌曲变更处理完成 =====");
            }
        }

        /// <summary>
        /// 歌词行变更时更新显示
        /// </summary>
        private void OnLyricLineChanged(object? sender, LyricsLineChangeEventArgs e)
        {
            CurrentLyrics = new Lyrics
            {
                Text = e.Line.Text,
                NextText = e.NextLine?.Text ?? "",
                Artist = e.SongInfo.FullTitle
            };
        }

        /// <summary>
        /// 手动重置歌词同步（用于拖动进度条后）
        /// </summary>
        public void ResetLyricsSync()
        {
            System.Diagnostics.Debug.WriteLine("[ViewModel] 手动重置歌词同步");

            // 重置跟踪器
            _progressTracker.Reset();

            // 重置并启动歌词同步
            StopLyricsSync();
            StartLyricsSync();

            System.Diagnostics.Debug.WriteLine("[ViewModel] ✓ 已重置，从 0 秒开始计时");
        }

        /// <summary>
        /// 手动设置播放进度（用于精确同步）
        /// </summary>
        /// <param name="progressSeconds">播放进度（秒）</param>
        public void SetProgress(double progressSeconds)
        {
            System.Diagnostics.Debug.WriteLine($"[ViewModel] 手动设置进度: {progressSeconds:F2}秒");

            // 更新跟踪器
            _progressTracker.ManualSetProgress(progressSeconds);

            // 立即更新歌词显示
            _lyricsPlayer.UpdateByTime(progressSeconds);

            StatusMessage = $"已同步到 {progressSeconds:F0}秒";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
