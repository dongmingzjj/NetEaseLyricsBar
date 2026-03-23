using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NetEaseLyricsBar.Models;

namespace NetEaseLyricsBar.Services
{
    /// <summary>
    /// LRC歌词解析器
    /// </summary>
    public class LrcParser
    {
        // LRC时间标签正则表达式：[mm:ss.xx] 或 [mm:ss]
        private static readonly Regex TimeTagRegex = new Regex(@"\[(\d{2}):(\d{2})(?:\.(\d{2,3}))?\]");

        /// <summary>
        /// 解析LRC格式歌词
        /// </summary>
        /// <param name="lrcText">LRC歌词文本</param>
        /// <returns>解析后的歌词数据</returns>
        public static LyricsData Parse(string lrcText)
        {
            System.Diagnostics.Debug.WriteLine("[LRC] ===== 开始解析 LRC =====");

            var lyricsData = new LyricsData();

            if (string.IsNullOrWhiteSpace(lrcText))
            {
                System.Diagnostics.Debug.WriteLine("[LRC] ✗ LRC文本为空");
                return lyricsData;
            }

            System.Diagnostics.Debug.WriteLine($"[LRC] 原始文本预览: {lrcText.Substring(0, Math.Min(200, lrcText.Length))}...");

            var lines = new List<LrcLine>();
            var linesList = lrcText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            System.Diagnostics.Debug.WriteLine($"[LRC] 共 {linesList.Length} 行文本");

            foreach (var line in linesList)
            {
                var parsedLines = ParseLine(line.Trim());
                if (parsedLines != null && parsedLines.Count > 0)
                {
                    lines.AddRange(parsedLines);
                }
            }

            // 按时间排序
            lines.Sort((a, b) => a.Time.CompareTo(b.Time));
            lyricsData.Lines = lines;

            System.Diagnostics.Debug.WriteLine($"[LRC] ✓ 解析完成，共 {lines.Count} 行歌词");
            System.Diagnostics.Debug.WriteLine("[LRC] ===== LRC解析完成 =====");

            return lyricsData;
        }

        /// <summary>
        /// 解析单行LRC歌词
        /// 支持同一行有多个时间标签的情况，如：[00:01.00][00:02.00]歌词文本
        /// </summary>
        /// <param name="line">LRC行</param>
        /// <returns>解析出的歌词行列表</returns>
        private static List<LrcLine>? ParseLine(string line)
        {
            var result = new List<LrcLine>();

            if (string.IsNullOrWhiteSpace(line))
            {
                return result;
            }

            // 提取所有时间标签
            var matches = TimeTagRegex.Matches(line);
            if (matches.Count == 0)
            {
                return null;
            }

            // 提取歌词文本（移除所有时间标签后的内容）
            var text = TimeTagRegex.Replace(line, "").Trim();

            // 跳过空歌词和元数据标签（如：[ti:标题]、[ar:歌手]等）
            if (string.IsNullOrEmpty(text) || line.Contains("[ti:") || line.Contains("[ar:") ||
                line.Contains("[al:") || line.Contains("[by:") || line.Contains("[offset:"))
            {
                return null;
            }

            // 为每个时间标签创建一个LrcLine
            foreach (Match match in matches)
            {
                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var milliseconds = match.Groups[3].Success ?
                    int.Parse(match.Groups[3].Value.PadRight(3, '0')) : 0;

                var time = minutes * 60 + seconds + milliseconds / 1000.0;
                result.Add(new LrcLine(time, text));
            }

            return result;
        }

        /// <summary>
        /// 将时间转换为LRC时间标签格式 [mm:ss.xx]
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <returns>LRC时间标签</returns>
        public static string FormatTime(double time)
        {
            var minutes = (int)(time / 60);
            var seconds = (int)(time % 60);
            var milliseconds = (int)((time % 1) * 100);
            return $"[{minutes:D2}:{seconds:D2}.{milliseconds:D2}]";
        }
    }
}
