using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace NetEaseLyricsBar.Services
{
    /// <summary>
    /// 字体管理服务 - 从文件系统安全加载字体
    /// </summary>
    public class FontManagerService
    {
        private const string FontFamilyName = "Maple Mono Normal";
        private static readonly string FontsFolderPath;
        private static Dictionary<string, bool> _fontAvailabilityCache = new Dictionary<string, bool>();

        static FontManagerService()
        {
            // 获取字体文件夹路径（相对于应用程序目录）
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            FontsFolderPath = Path.Combine(appDir, "Fonts");
        }

        // 验证字体文件是否存在
        public static bool ValidateFontFile(string fontFileName)
        {
            if (_fontAvailabilityCache.ContainsKey(fontFileName))
            {
                return _fontAvailabilityCache[fontFileName];
            }

            var fontPath = Path.Combine(FontsFolderPath, fontFileName);
            var exists = File.Exists(fontPath);
            _fontAvailabilityCache[fontFileName] = exists;

            return exists;
        }

        // 清除缓存（用于调试）
        public static void ClearFontCache()
        {
            _fontAvailabilityCache.Clear();
        }

        /// <summary>
        /// 获取 Maple 字体家族（安全版本）
        /// </summary>
        public static FontFamily GetMapleFontFamily(string weight, bool italic)
        {
            try
            {
                var fontFileName = GetFontFileName(weight, italic);

                // 验证字体文件存在
                if (!ValidateFontFile(fontFileName))
                {
                    System.Diagnostics.Debug.WriteLine($"字体文件不存在: {fontFileName}");
                    return null;
                }

                // 使用文件路径加载字体（不使用嵌入式资源）
                var fontPath = Path.Combine(FontsFolderPath, fontFileName);
                return new FontFamily(new Uri(fontPath), "./#" + FontFamilyName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 Maple 字体失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取字体文件名
        /// </summary>
        private static string GetFontFileName(string weight, bool italic)
        {
            string weightSuffix = weight switch
            {
                "Thin" => "Thin",
                "ExtraLight" => "ExtraLight",
                "Light" => "Light",
                "Regular" => "Regular",
                "Medium" => "Medium",
                "SemiBold" => "SemiBold",
                "Bold" => "Bold",
                "ExtraBold" => "ExtraBold",
                _ => "Regular"
            };

            string italicSuffix = italic ? "Italic" : "";
            return $"MapleMonoNormal-{weightSuffix}{italicSuffix}.ttf";
        }

        /// <summary>
        /// 获取字体粗细
        /// </summary>
        public static System.Windows.FontWeight GetFontWeight(string weight)
        {
            return weight switch
            {
                "Thin" => System.Windows.FontWeights.Thin,
                "ExtraLight" => System.Windows.FontWeights.ExtraLight,
                "Light" => System.Windows.FontWeights.Light,
                "Regular" => System.Windows.FontWeights.Regular,
                "Medium" => System.Windows.FontWeights.Medium,
                "SemiBold" => System.Windows.FontWeights.SemiBold,
                "Bold" => System.Windows.FontWeights.Bold,
                "ExtraBold" => System.Windows.FontWeights.ExtraBold,
                _ => System.Windows.FontWeights.SemiBold
            };
        }

        /// <summary>
        /// 获取所有字体选项
        /// </summary>
        public static List<FontOption> GetAllFontOptions()
        {
            var options = new List<FontOption>();
            string[] weights = { "Thin", "ExtraLight", "Light", "Regular", "Medium", "SemiBold", "Bold", "ExtraBold" };

            foreach (var weight in weights)
            {
                // 正体
                options.Add(new FontOption
                {
                    DisplayName = GetLocalizedWeightName(weight),
                    Weight = weight,
                    Italic = false,
                    FontFamily = GetMapleFontFamily(weight, false) ?? GetDefaultFontFamily(),
                    FontWeightValue = GetFontWeight(weight)
                });

                // 斜体
                options.Add(new FontOption
                {
                    DisplayName = GetLocalizedWeightName(weight) + " 斜体",
                    Weight = weight,
                    Italic = true,
                    FontFamily = GetMapleFontFamily(weight, true) ?? GetDefaultFontFamily(),
                    FontWeightValue = GetFontWeight(weight)
                });
            }

            return options;
        }

        /// <summary>
        /// 获取默认字体（如果 Maple 不可用）
        /// </summary>
        public static FontFamily GetDefaultFontFamily()
        {
            return new FontFamily("Microsoft YaHei UI");
        }

        /// <summary>
        /// 获取本地化的字重名称
        /// </summary>
        private static string GetLocalizedWeightName(string weight)
        {
            return weight switch
            {
                "Thin" => "极细",
                "ExtraLight" => "特细",
                "Light" => "细体",
                "Regular" => "常规",
                "Medium" => "中等",
                "SemiBold" => "半粗",
                "Bold" => "粗体",
                "ExtraBold" => "特粗",
                _ => weight
            };
        }
    }

    /// <summary>
    /// 字体选项类
    /// </summary>
    public class FontOption
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public bool Italic { get; set; }
        public FontFamily FontFamily { get; set; } = null!;
        public System.Windows.FontWeight FontWeightValue { get; set; }
    }
}
