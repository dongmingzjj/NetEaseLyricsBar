namespace NetEaseLyricsBar.Models
{
    /// <summary>
    /// LRC歌词行
    /// </summary>
    public class LrcLine
    {
        /// <summary>
        /// 时间戳（秒）
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// 歌词文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        public LrcLine(double time, string text)
        {
            Time = time;
            Text = text;
        }

        public override string ToString()
        {
            return $"[{Time:F2}] {Text}";
        }
    }
}
