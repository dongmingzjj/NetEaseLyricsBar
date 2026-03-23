using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;

namespace NetEaseLyricsBar.Models
{
    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppConfig : INotifyPropertyChanged
    {
        private bool _useMemoryReading = true;
        private bool _useAutoScan = false;
        private string _memoryConfigFile = "netease_memory_config.json";

        public bool UseMemoryReading
        {
            get => _useMemoryReading;
            set
            {
                _useMemoryReading = value;
                OnPropertyChanged();
                Save();
            }
        }

        public bool UseAutoScan
        {
            get => _useAutoScan;
            set
            {
                _useAutoScan = value;
                OnPropertyChanged();
                Save();
            }
        }

        public string MemoryConfigFile
        {
            get => _memoryConfigFile;
            set
            {
                _memoryConfigFile = value;
                OnPropertyChanged();
                Save();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static readonly string ConfigPath = Path.Combine(
            AppContext.BaseDirectory,
            "app_config.json"
        );

        /// <summary>
        /// 加载配置
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[配置] 加载失败: {ex.Message}");
            }

            return new AppConfig();
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[配置] 保存失败: {ex.Message}");
            }
        }
    }
}
