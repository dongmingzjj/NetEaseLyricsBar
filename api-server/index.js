const express = require('express');
const cors = require('cors');
const axios = require('axios');

const app = express();
const PORT = 3000;

// 启用 CORS
app.use(cors());

// 网易云音乐 API 配置
const NETEASE_API_BASE = 'https://music.163.com';

/**
 * 搜索歌曲
 * GET /api/search?keywords=歌曲名&artist=歌手名
 */
async function searchSong(keywords, artist = '') {
    try {
        // 使用网易云音乐的搜索API（通过代理）
        const searchUrl = 'https://music.163.com/api/search/get/web';
        const params = {
            s: keywords,
            type: '1', // 单曲
            offset: 0,
            limit: 10
        };

        const response = await axios.get(searchUrl, {
            params,
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
                'Referer': 'https://music.163.com/'
            }
        });

        if (response.data.code === 200 && response.data.result?.songs) {
            const songs = response.data.result.songs;

            // 如果指定了歌手，尝试匹配
            if (artist) {
                const matched = songs.find(song =>
                    song.artists && song.artists.some(a =>
                        a.name.toLowerCase() === artist.toLowerCase()
                    )
                );
                if (matched) return matched;
            }

            // 返回第一个结果
            return songs[0];
        }

        return null;
    } catch (error) {
        console.error('搜索歌曲失败:', error.message);
        return null;
    }
}

/**
 * 获取歌词
 * GET /api/lyric?id=歌曲ID
 */
async function getLyric(songId) {
    try {
        const lyricUrl = 'https://music.163.com/api/song/lyric';
        const params = {
            id: songId,
            lv: 1, // 歌词版本
            kv: 1, // 翻译版本
            tv: 1  // 罗马音版本
        };

        const response = await axios.get(lyricUrl, {
            params,
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
                'Referer': 'https://music.163.com/'
            }
        });

        if (response.data.code === 200 && response.data.lrc) {
            return {
                lyric: response.data.lrc.lyric || '',
                nolyric: false
            };
        }

        return {
            lyric: '',
            nolyric: true
        };
    } catch (error) {
        console.error('获取歌词失败:', error.message);
        return {
            lyric: '',
            nolyric: false
        };
    }
}

/**
 * 一站式搜索并获取歌词
 * GET /search-and-lyric?keywords=歌曲名&artist=歌手名
 */
app.get('/search-and-lyric', async (req, res) => {
    const { keywords, artist } = req.query;

    if (!keywords) {
        return res.status(400).json({
            code: 400,
            message: '缺少 keywords 参数'
        });
    }

    try {
        console.log(`[${new Date().toLocaleTimeString()}] 搜索: ${keywords} - ${artist || ''}`);

        // 1. 搜索歌曲
        const song = await searchSong(keywords, artist);

        if (!song) {
            return res.json({
                code: 404,
                message: '未找到歌曲',
                data: null
            });
        }

        console.log(`  → 找到歌曲: ${song.name} - ${song.artists?.map(a => a.name).join('/')}`);

        // 2. 获取歌词
        const lyricData = await getLyric(song.id);

        if (lyricData.nolyric || !lyricData.lyric) {
            console.log(`  → 无歌词（纯音乐）`);
            return res.json({
                code: 200,
                message: 'success',
                data: {
                    nolyric: true,
                    lyric: '',
                    song: {
                        id: song.id.toString(),
                        name: song.name,
                        artist: song.artists?.map(a => a.name).join('/') || ''
                    }
                }
            });
        }

        console.log(`  → 歌词长度: ${lyricData.lyric.length} 字符`);

        // 3. 返回结果
        res.json({
            code: 200,
            message: 'success',
            data: {
                lyric: lyricData.lyric,
                nolyric: false,
                song: {
                    id: song.id.toString(),
                    name: song.name,
                    artist: song.artists?.map(a => a.name).join('/') || ''
                }
            }
        });

    } catch (error) {
        console.error('处理请求失败:', error.message);
        res.status(500).json({
            code: 500,
            message: '服务器错误',
            error: error.message
        });
    }
});

/**
 * 健康检查
 */
app.get('/health', (req, res) => {
    res.json({
        code: 200,
        message: 'NetEase Lyrics API Server is running',
        timestamp: new Date().toISOString()
    });
});

/**
 * 启动服务器
 */
app.listen(PORT, () => {
    console.log('=================================');
    console.log('NetEase Lyrics API Server');
    console.log('=================================');
    console.log(`✓ 服务运行在: http://localhost:${PORT}`);
    console.log(`✓ 健康检查: http://localhost:${PORT}/health`);
    console.log(`✓ 歌词接口: http://localhost:${PORT}/search-and-lyric?keywords=歌曲名&artist=歌手名`);
    console.log('=================================');
    console.log('按 Ctrl+C 停止服务器');
    console.log('=================================');
});
