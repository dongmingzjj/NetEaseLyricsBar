using System.Collections.Generic;
using System.Linq;

namespace NetEaseLyricsBar.Models
{
    /// <summary>
    /// 完整歌词数据
    /// </summary>
    public class LyricsData
    {
        /// <summary>
        /// 歌曲信息
        /// </summary>
        public SongInfo SongInfo { get; set; } = new SongInfo();

        /// <summary>
        /// 歌词行列表（按时间排序）
        /// </summary>
        public List<LrcLine> Lines { get; set; } = new List<LrcLine>();

        /// <summary>
        /// 是否为空（没有歌词）
        /// </summary>
        public bool IsEmpty => Lines.Count == 0;

        /// <summary>
        /// 获取指定时间的歌词行
        /// </summary>
        /// <param name="currentTime">当前时间（秒）</param>
        /// <returns>当前歌词行，如果找不到返回null</returns>
        public LrcLine? GetLineAtTime(double currentTime)
        {
            if (IsEmpty) return null;

            // 查找最后一个时间 <= currentTime 的歌词行
            for (int i = Lines.Count - 1; i >= 0; i--)
            {
                if (Lines[i].Time <= currentTime)
                {
                    return Lines[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定时间歌词行的索引
        /// </summary>
        /// <param name="currentTime">当前时间（秒）</param>
        /// <returns>索引，如果找不到返回-1</returns>
        public int GetLineIndexAtTime(double currentTime)
        {
            if (IsEmpty) return -1;

            for (int i = Lines.Count - 1; i >= 0; i--)
            {
                if (Lines[i].Time <= currentTime)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
