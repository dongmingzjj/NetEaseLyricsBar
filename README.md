# NetEaseLyricsBar

![License](https://img.shields.io/badge/license-非商业性使用-red.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![WPF](https://img.shields.io/badge/WPF-Windows%2010%2B-blue.svg)

> **网易云音乐桌面歌词栏** - 一个极简的网易云 PC 客户端歌词显示工具

[English](README_EN.md) | 简体中文

---

## ✨ 特性

- 🎵 **实时歌词同步** - 双行显示当前歌词和下一句
- ⚡ **智能延迟补偿** - 自动补偿检测延迟和加载延迟
- 🖥️ **任务栏集成** - 自动适应任务栏位置和大小
- 🔒 **位置锁定** - 防止误操作拖动
- 🎨 **毛玻璃效果** - 透明窗口，美观不遮挡
- 🚀 **零配置启动** - 开箱即用，无需额外配置

---

## 📸 预览

```
┌────────────────────────────────────────────────────┐
│  当前歌词（大字醒目）  歌手  下一句（小字次要）  │
└────────────────────────────────────────────────────┘
```

---

## 🚀 快速开始

### 环境要求

- Windows 10 1809+ (版本 10.0.26100.0+)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (用于运行 API 服务器)
- 网易云音乐 PC 客户端

### 启动步骤

#### 1. 启动歌词 API 服务器

```bash
cd api-server
npm install
npm start
```

服务器将运行在 `http://localhost:3000`

#### 2. 启动歌词栏

```bash
cd NetEaseLyricsBar
dotnet run
```

或编译发布：
```bash
dotnet build -c Release
```

### 完整启动脚本

创建 `start.bat`：
```batch
@echo off
start /min cmd /c "cd api-server && npm install && npm start"
timeout /t 3
cd NetEaseLyricsBar
dotnet run
```

---

## 📖 使用说明

### 基本操作

1. **启动程序**
   - 确保网易云音乐 PC 客户端正在运行
   - 运行 NetEaseLyricsBar.exe
   - 程序自动检测歌曲并显示歌词

2. **调整位置**
   - 鼠标拖动歌词栏到任务栏任意位置
   - 窗口自动适应任务栏高度

3. **右键菜单**
   - `锁定位置` - 固定当前位置，防止误拖
   - `重置位置` - 恢复到任务栏左侧默认位置
   - `关于` - 查看版本信息
   - `退出` - 关闭程序

4. **快捷键**
   - `Ctrl + Alt + R` - 重置窗口位置

### 工作原理

```
网易云音乐播放
    ↓
监听器检测歌曲变更（每2秒）
    ↓
获取歌词并计算延迟补偿
    ↓
双行显示：当前歌词 + 下一句
```

### 延迟补偿说明

程序会自动补偿两种延迟：

1. **检测延迟** - 监听器每2秒检查一次，平均延迟1秒
2. **加载延迟** - 网络请求获取歌词，约300-1000ms

总延迟 = 检测延迟 + 加载延迟（约1-3秒）

---

## 🛠️ 技术栈

### WPF 客户端
- **框架**: .NET 8.0 + WPF
- **语言**: C# 12
- **API**: Windows API (user32.dll)
- **架构**: MVVM 模式
- **异步**: async/await
- **定时器**: DispatcherTimer

### API 服务器
- **运行时**: Node.js 18+
- **框架**: Express.js
- **依赖**:
  - `express` - Web 服务器
  - `cors` - 跨域支持
  - `axios` - HTTP 客户端

### 核心技术

**WPF 客户端**:
- **窗口监听** - Win32 API `EnumWindows` 枚举窗口
- **任务栏检测** - `FindWindow` 查找任务栏窗口
- **强制置顶** - `SetWindowPos` 设置 `HWND_TOPMOST`
- **歌词解析** - LRC 格式解析
- **延迟补偿** - DateTime 精确计时

**API 服务器**:
- **搜索接口** - 网易云音乐搜索 API
- **歌词接口** - 网易云音乐歌词 API
- **一站式接口** - `/search-and-lyric` 合并搜索和获取

---

## 📂 项目结构

```
NetEaseLyricsBar/
├── NetEaseLyricsBar/       # WPF 客户端
│   ├── Models/              # 数据模型
│   │   ├── SongInfo.cs     # 歌曲信息
│   │   └── Lyrics.cs       # 歌词模型
│   ├── Services/           # 核心服务
│   │   ├── NeteaseMusicMonitor.cs      # 网易云监听
│   │   ├── NeteaseApiService.cs        # 歌词API
│   │   ├── LyricsPlayerService.cs      # 歌词播放器
│   │   ├── SmartProgressTracker.cs     # 进度跟踪器
│   │   └── TaskbarService.cs           # 任务栏服务
│   ├── ViewModels/         # 视图模型
│   │   └── MainViewModel.cs
│   ├── Styles/             # 样式资源
│   │   └── MaterialDesign3.xaml
│   ├── Properties/         # 程序集信息
│   ├── App.xaml            # 应用程序入口
│   ├── MainWindow.xaml     # 主窗口UI
│   └── NetEaseLyricsBar.csproj
│
└── api-server/             # Node.js 歌词服务器
    ├── index.js            # 服务器主文件
    ├── package.json        # 依赖配置
    └── node_modules/       # 依赖包（git忽略）
```

---

## ⚙️ 配置说明

### API 服务器

**地址**: `http://localhost:3000`

**接口列表**:

| 接口 | 方法 | 参数 | 说明 |
|------|------|------|------|
| `/search-and-lyric` | GET | `keywords`, `artist` | 搜索并获取歌词（一站式） |
| `/health` | GET | - | 健康检查 |

**响应格式**:
```json
{
  "code": 200,
  "message": "success",
  "data": {
    "lyric": "[00:00.00] 歌词内容...",
    "nolyric": false,
    "song": {
      "id": "12345",
      "name": "歌曲名",
      "artist": "歌手名"
    }
  }
}
```

**启动方式**:
```bash
cd api-server
npm install  # 首次运行需要安装依赖
npm start    # 启动服务器
```

### 窗口设置

程序自动保存以下配置到 `App.config`：

- 窗口位置（X, Y）
- 窗口大小（Width, Height）
- 锁定状态

使用 [DebugView](https://learn.microsoft.com/en-us/sysinternals/downloads/debugview) 查看调试输出：

```
[ViewModel] ===== 歌曲变更 =====
[监听] ✓ 找到网易云窗口: 夜曲 - 周杰伦
[ViewModel] ⏱ 检测延迟: 850ms
[ViewModel] ⏱ 加载延迟: 420ms
[ViewModel] ⏱ 总延迟: 1270ms
[Player] 歌词行变更: [1/45] 为你弹奏肖邦的夜曲
```

---

## 🐛 已知问题

1. **歌词进度偏差**
   - 原因：检测延迟和加载延迟
   - 解决：已实现自动延迟补偿
   - 注意：拖动进度条后需要手动重启歌词同步

2. **任务栏覆盖**
   - 已通过 Win32 API 强制置顶解决
   - 每2秒自动检测并重新置顶

3. **任务栏图标变化**
   - 每2秒自动检测任务栏空位
   - 动态调整窗口宽度，不覆盖图标

---

## 🔒 许可证

**本项目仅供学习交流使用，未经作者授权不得用于商业用途。**

- ✅ 个人学习使用
- ✅ 非商业性用途
- ❌ 商业用途（需联系作者获取授权）

---

## 🙏 致谢

- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) - UI 设计灵感
- 网易云音乐 - 音乐数据源

---

## 📮 联系方式

- GitHub: [@dongmingzjj](https://github.com/dongmingzjj)
- 项目地址: [https://github.com/dongmingzjj/NetEaseLyricsBar](https://github.com/dongmingzjj/NetEaseLyricsBar)

---

## 📝 更新日志

### v1.0.0 (2026-03-23)

- ✨ 首次发布
- 🎵 双行歌词显示
- ⚡ 智能延迟补偿
- 🖥️ 任务栏自动适应
- 🔒 位置锁定功能
- 🎨 毛玻璃透明效果
