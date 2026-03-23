const express = require('express');
const cors = require('cors');
const { default: NeteaseCloudMusicApi } = require('NeteaseCloudMusicApi');

const app = express();
const PORT = 3000;

// 启用 CORS
app.use(cors());

// 初始化网易云音乐 API（如果需要使用代理，可以在这里配置）
const neteaseApi = new NeteaseCloudMusicApi({
    cookie: '', // 如果需要登录后访问的功能，可以在这里设置 cookie
});

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
        const searchResult = await neteaseApi.searchSong({
            keywords: keywords,
            limit: 10
        });

        if (!searchResult || !searchResult.result || !searchResult.result.songs || searchResult.result.songs.length === 0) {
            return res.json({
                code: 404,
                message: '未找到歌曲',
                data: null
            });
        }

        const songs = searchResult.result.songs;

        // 2. 如果指定了歌手，尝试匹配
        let selectedSong = songs[0];

        if (artist) {
            const matched = songs.find(song =>
                song.ar && song.ar.some(a =>
                    a.name.toLowerCase() === artist.toLowerCase()
                )
            );
            if (matched) selectedSong = matched;
        }

        console.log(`  → 找到歌曲: ${selectedSong.name} - ${selectedSong.ar?.map(a => a.name).join('/')}`);

        // 3. 获取歌词
        const lyricResult = await neteaseApi.songLyric({
            id: selectedSong.id
        });

        if (!lyricResult || !lyricResult.lrc) {
            console.log(`  → 无歌词数据`);
            return res.json({
                code: 200,
                message: 'success',
                data: {
                    nolyric: false,
                    lyric: '',
                    song: {
                        id: selectedSong.id.toString(),
                        name: selectedSong.name,
                        artist: selectedSong.ar?.map(a => a.name).join('/') || ''
                    }
                }
            });
        }

        const lrcText = lyricResult.lrc.lyric || '';

        // 检查是否纯音乐
        if (!lrcText || lyricResult.nolyric) {
            console.log(`  → 无歌词（纯音乐）`);
            return res.json({
                code: 200,
                message: 'success',
                data: {
                    nolyric: true,
                    lyric: '',
                    song: {
                        id: selectedSong.id.toString(),
                        name: selectedSong.name,
                        artist: selectedSong.ar?.map(a => a.name).join('/') || ''
                    }
                }
            });
        }

        console.log(`  → 歌词长度: ${lrcText.length} 字符`);

        // 4. 返回结果
        res.json({
            code: 200,
            message: 'success',
            data: {
                lyric: lrcText,
                nolyric: false,
                song: {
                    id: selectedSong.id.toString(),
                    name: selectedSong.name,
                    artist: selectedSong.ar?.map(a => a.name).join('/') || ''
                }
            }
        });

    } catch (error) {
        console.error('处理请求失败:', error.message);
        console.error('错误详情:', error);
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
