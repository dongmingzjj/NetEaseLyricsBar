using System;
using NetEaseLyricsBar.Models;

namespace NetEaseLyricsBar.Services
{
    /// <summary>
    /// 歌词播放器服务
    /// </summary>
    public class LyricsPlayerService
    {
        private LyricsData? _currentLyrics;
        private int _currentIndex = -1;

        /// <summary>
        /// 歌词行变更事件
        /// </summary>
        public event EventHandler<LyricsLineChangeEventArgs>? LineChanged;

        /// <summary>
        /// 当前歌词数据
        /// </summary>
        public LyricsData? CurrentLyrics
        {
            get => _currentLyrics;
            private set
            {
                _currentLyrics = value;
                _currentIndex = -1;
            }
        }

        /// <summary>
        /// 加载新歌词
        /// </summary>
        /// <param name="lyricsData">歌词数据</param>
        public void LoadLyrics(LyricsData? lyricsData)
        {
            System.Diagnostics.Debug.WriteLine("[Player] ===== 加载歌词数据 =====");

            CurrentLyrics = lyricsData;
            _currentIndex = -1;

            if (lyricsData != null && !lyricsData.IsEmpty)
            {
                System.Diagnostics.Debug.WriteLine($"[Player] ✓ 歌词不为空，共 {lyricsData.Lines.Count} 行");
                if (lyricsData.Lines.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Player] 第一句: {lyricsData.Lines[0].Text}");
                    // 加载时显示第一句
                    UpdateCurrentLine(0);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Player] ⚠ 歌词为空或 null");
            }
        }

        /// <summary>
        /// 根据时间更新当前歌词行
        /// </summary>
        /// <param name="currentTime">当前播放时间（秒）</param>
        /// <returns>当前歌词行</returns>
        public LrcLine? UpdateByTime(double currentTime)
        {
            if (CurrentLyrics == null || CurrentLyrics.IsEmpty)
            {
                return null;
            }

            var newIndex = CurrentLyrics.GetLineIndexAtTime(currentTime);

            if (newIndex != -1 && newIndex != _currentIndex)
            {
                _currentIndex = newIndex;
                UpdateCurrentLine(_currentIndex);
            }

            return GetCurrentLine();
        }

        /// <summary>
        /// 切换到下一句
        /// </summary>
        /// <returns>下一句歌词</returns>
        public LrcLine? NextLine()
        {
            if (CurrentLyrics == null || CurrentLyrics.IsEmpty)
            {
                return null;
            }

            if (_currentIndex < CurrentLyrics.Lines.Count - 1)
            {
                _currentIndex++;
                UpdateCurrentLine(_currentIndex);
            }

            return GetCurrentLine();
        }

        /// <summary>
        /// 切换到上一句
        /// </summary>
        /// <returns>上一句歌词</returns>
        public LrcLine? PreviousLine()
        {
            if (CurrentLyrics == null || CurrentLyrics.IsEmpty)
            {
                return null;
            }

            if (_currentIndex > 0)
            {
                _currentIndex--;
                UpdateCurrentLine(_currentIndex);
            }

            return GetCurrentLine();
        }

        /// <summary>
        /// 获取当前歌词行
        /// </summary>
        /// <returns>当前歌词行</returns>
        public LrcLine? GetCurrentLine()
        {
            if (CurrentLyrics == null || CurrentLyrics.IsEmpty || _currentIndex < 0)
            {
                return null;
            }

            return CurrentLyrics.Lines[_currentIndex];
        }

        /// <summary>
        /// 更新当前歌词行并触发事件
        /// </summary>
        private void UpdateCurrentLine(int index)
        {
            if (CurrentLyrics == null || CurrentLyrics.IsEmpty)
            {
                return;
            }

            if (index >= 0 && index < CurrentLyrics.Lines.Count)
            {
                var line = CurrentLyrics.Lines[index];

                // 获取下一行歌词
                LrcLine? nextLine = null;
                if (index + 1 < CurrentLyrics.Lines.Count)
                {
                    nextLine = CurrentLyrics.Lines[index + 1];
                }

                System.Diagnostics.Debug.WriteLine($"[Player] 歌词行变更: [{index + 1}/{CurrentLyrics.Lines.Count}] {line.Text}");
                if (nextLine != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Player] 下一句: {nextLine.Text}");
                }

                LineChanged?.Invoke(this, new LyricsLineChangeEventArgs
                {
                    Line = line,
                    NextLine = nextLine,
                    SongInfo = CurrentLyrics.SongInfo,
                    Index = index,
                    TotalLines = CurrentLyrics.Lines.Count
                });
            }
        }

        /// <summary>
        /// 重置播放器状态
        /// </summary>
        public void Reset()
        {
            _currentIndex = -1;
        }
    }

    /// <summary>
    /// 歌词行变更事件参数
    /// </summary>
    public class LyricsLineChangeEventArgs : EventArgs
    {
        /// <summary>
        /// 歌词行
        /// </summary>
        public LrcLine Line { get; set; } = null!;

        /// <summary>
        /// 下一句歌词行
        /// </summary>
        public LrcLine? NextLine { get; set; }

        /// <summary>
        /// 歌曲信息
        /// </summary>
        public SongInfo SongInfo { get; set; } = null!;

        /// <summary>
        /// 当前索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 总行数
        /// </summary>
        public int TotalLines { get; set; }
    }
}
