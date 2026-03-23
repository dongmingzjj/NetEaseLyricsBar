using System.Windows;

namespace NetEaseLyricsBar.Models
{
    public class WindowSettings
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsLocked { get; set; }
    }

    public class Settings
    {
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();
        public string AnimationMode { get; set; } = "Fade";
    }
}
