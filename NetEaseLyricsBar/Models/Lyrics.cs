namespace NetEaseLyricsBar.Models
{
    public class Lyrics
    {
        public string Text { get; set; } = string.Empty;
        public string NextText { get; set; } = string.Empty; // 下一句歌词
        public string Artist { get; set; } = string.Empty;
    }
}
