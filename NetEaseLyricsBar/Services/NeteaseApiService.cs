using System;
using System.Net.Http;
using System.Threading.Tasks;
using NetEaseLyricsBar.Models;
using System.Text.Json;

namespace NetEaseLyricsBar.Services
{
    /// <summary>
    /// 网易云音乐API服务（使用本地 API 服务）
    /// </summary>
    public class NeteaseApiService
    {
        private readonly HttpClient _httpClient;
        private const string LocalApiUrl = "http://localhost:3000/";

        public NeteaseApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// 一站式获取歌词数据
        /// </summary>
        /// <param name="songName">歌曲名称</param>
        /// <param name="artist">歌手名称</param>
        /// <returns>歌词数据，如果失败返回null</returns>
        public async Task<LyricsData?> FetchLyricsAsync(string songName, string artist)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[API] ===== 开始获取歌词 =====");
                System.Diagnostics.Debug.WriteLine($"[API] 歌名: {songName}, 歌手: {artist}");

                // 使用本地 API 的一站式接口
                var url = $"{LocalApiUrl}search-and-lyric?keywords={Uri.EscapeDataString(songName)}&artist={Uri.EscapeDataString(artist)}";
                System.Diagnostics.Debug.WriteLine($"[API] 正在调用本地 API: {url}");

                var response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrEmpty(response))
                {
                    System.Diagnostics.Debug.WriteLine("[API] ✗ API返回空结果");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[API] ✓ 响应: {response.Substring(0, Math.Min(500, response.Length))}...");

                // 解析 JSON 响应
                var jsonDoc = JsonDocument.Parse(response);

                // 检查状态码
                if (!jsonDoc.RootElement.TryGetProperty("code", out var codeProp) || codeProp.GetInt32() != 200)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ API返回错误: {response}");
                    return null;
                }

                // 提取数据
                if (!jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ 响应中没有 data 字段");
                    return null;
                }

                // 检查是否有歌词
                bool nolyric = false;
                if (data.TryGetProperty("nolyric", out var nolyricProp))
                {
                    nolyric = nolyricProp.GetBoolean();
                }

                if (nolyric)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ⚠ 歌曲无歌词（纯音乐）");
                    return null;
                }

                // 提取歌词文本
                if (!data.TryGetProperty("lyric", out var lyricProp))
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ 响应中没有 lyric 字段");
                    return null;
                }

                var lrcText = lyricProp.GetString();
                if (string.IsNullOrEmpty(lrcText))
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ 歌词文本为空");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[API] ✓ 歌词文本长度: {lrcText.Length}");

                // 解析LRC格式
                var lyricsData = LrcParser.Parse(lrcText);

                // 获取歌曲信息
                if (data.TryGetProperty("song", out var songProp))
                {
                    lyricsData.SongInfo = new SongInfo
                    {
                        Id = songProp.TryGetProperty("id", out var idProp) ? idProp.ToString() : "",
                        Name = songProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? songName : songName,
                        Artist = songProp.TryGetProperty("artist", out var artistProp) ? artistProp.GetString() ?? artist : artist
                    };
                }
                else
                {
                    lyricsData.SongInfo = new SongInfo
                    {
                        Id = "",
                        Name = songName,
                        Artist = artist
                    };
                }

                System.Diagnostics.Debug.WriteLine($"[API] ✓ LRC解析完成，共{lyricsData.Lines.Count}行歌词");
                System.Diagnostics.Debug.WriteLine($"[API] ===== 歌词获取完成 =====");

                return lyricsData;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] ✗ 网络请求失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[API] ⚠ 请确保本地 API 服务已启动 (http://localhost:3000)");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] ✗ 请求超时: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[API] ⚠ 请确保本地 API 服务已启动 (http://localhost:3000)");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] ✗ 获取歌词失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[API] 异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[API] 异常详情: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 从搜索结果JSON中提取歌曲ID
        /// </summary>
        private string? ExtractSongIdFromSearchJson(string searchJson, string songName, string artist)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(searchJson);
                JsonElement songsArray;

                // Meting API 返回格式：data[] 或直接是数组
                // 尝试获取歌曲数组
                if (jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    System.Diagnostics.Debug.WriteLine($"[API] JSON结构: data.{data.ValueKind}");

                    if (data.ValueKind == JsonValueKind.Array)
                    {
                        songsArray = data;
                    }
                    else if (data.TryGetProperty("songs", out var songs))
                    {
                        songsArray = songs;
                        System.Diagnostics.Debug.WriteLine($"[API] JSON结构: data.songs");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[API] ✗ 未找到歌曲数组");
                        return null;
                    }
                }
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    songsArray = jsonDoc.RootElement;
                    System.Diagnostics.Debug.WriteLine($"[API] JSON结构: 直接数组");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ 未知JSON结构: {jsonDoc.RootElement.ValueKind}");
                    return null;
                }

                if (songsArray.GetArrayLength() == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ 歌曲数组为空");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[API] 共找到 {songsArray.GetArrayLength()} 首歌曲");

                string? firstSongId = null;

                foreach (var song in songsArray.EnumerateArray())
                {
                    // 保存第一个结果的ID作为容错
                    if (firstSongId == null && song.TryGetProperty("id", out var firstId))
                    {
                        firstSongId = firstId.ToString();
                    }

                    // 尝试匹配歌手
                    bool artistMatched = false;
                    if (song.TryGetProperty("artist", out var artistName))
                    {
                        if (artistName.GetString()?.Equals(artist, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            artistMatched = true;
                        }
                    }
                    else if (song.TryGetProperty("ar", out var artists))
                    {
                        // ar可能是数组
                        if (artists.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var artistObj in artists.EnumerateArray())
                            {
                                if (artistObj.TryGetProperty("name", out var artistNameStr) &&
                                    artistNameStr.GetString()?.Equals(artist, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    artistMatched = true;
                                    break;
                                }
                            }
                        }
                    }

                    // 歌手匹配，返回该歌曲ID
                    if (artistMatched && song.TryGetProperty("id", out var id))
                    {
                        System.Diagnostics.Debug.WriteLine($"[API] ✓ 找到精确匹配的歌曲ID: {id}");
                        return id.ToString();
                    }
                }

                // 容错：如果没有匹配的歌手，返回第一个结果
                if (firstSongId != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ⚠ 未找到精确匹配的歌曲，使用第一个结果ID: {firstSongId}");
                    return firstSongId;
                }

                System.Diagnostics.Debug.WriteLine($"[API] ✗ 未找到任何歌曲ID");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] ✗ 解析搜索结果失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从歌词结果JSON中提取LRC文本
        /// </summary>
        private string? ExtractLyricFromJson(string lyricJson)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(lyricJson);

                // Meting API 返回格式：data.lrc.lyric 或直接 lrc.lyric
                JsonElement lrcElement;

                if (jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    System.Diagnostics.Debug.WriteLine($"[API] JSON结构: data.{data.ValueKind}");
                    if (data.TryGetProperty("lrc", out lrcElement))
                    {
                        System.Diagnostics.Debug.WriteLine($"[API] ✓ 找到 data.lrc");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[API] ✗ data中未找到lrc字段");
                        return null;
                    }
                }
                else if (jsonDoc.RootElement.TryGetProperty("lrc", out lrcElement))
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✓ 找到直接lrc");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ 未找到lrc字段");
                    return null;
                }

                // 提取歌词文本
                if (lrcElement.TryGetProperty("lyric", out var lyric))
                {
                    var lyricText = lyric.GetString();
                    if (!string.IsNullOrEmpty(lyricText))
                    {
                        System.Diagnostics.Debug.WriteLine($"[API] ✓ 提取到歌词文本");
                        return lyricText;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[API] ⚠ lyric字段为空");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[API] ✗ lrc中未找到lyric字段");
                }

                // 检查是否没有歌词
                if (jsonDoc.RootElement.TryGetProperty("nolyric", out var nolyric) &&
                    nolyric.GetBoolean())
                {
                    System.Diagnostics.Debug.WriteLine("[API] ⚠ 歌曲无歌词（纯音乐）");
                    return null;
                }

                // 检查是否未收录
                if (jsonDoc.RootElement.TryGetProperty("uncollected", out var uncollected) &&
                    uncollected.GetBoolean())
                {
                    System.Diagnostics.Debug.WriteLine("[API] ⚠ 歌词未收录");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[API] ✗ 未知原因：未能提取歌词");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] ✗ 解析歌词结果失败: {ex.Message}");
                return null;
            }
        }
    }
}
