namespace NetEaseLyricsBar.Models
{
    /// <summary>
    /// 歌曲信息
    /// </summary>
    public class SongInfo
    {
        /// <summary>
        /// 歌曲ID（网易云ID）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 歌曲名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 歌手名称
        /// </summary>
        public string Artist { get; set; } = string.Empty;

        /// <summary>
        /// 歌曲开始播放时间（用于延迟补偿）
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 完整标题（格式：歌名 - 歌手）
        /// </summary>
        public string FullTitle => $"{Name} - {Artist}";
    }
}
