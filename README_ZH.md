# 163MusicLyrics CLI

从网易云音乐、QQ 音乐下载歌词的命令行工具。

> Fork 自 [jitwxs/163MusicLyrics](https://github.com/jitwxs/163MusicLyrics)，去掉 GUI，以命令行形式对外提供核心功能。

[![Release](https://img.shields.io/github/v/release/liu246542/163MusicLyrics-CLI.svg)](https://github.com/liu246542/163MusicLyrics-CLI/releases)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

---

## 安装

从 [Releases](https://github.com/liu246542/163MusicLyrics-CLI/releases) 页面下载对应平台的二进制文件：

| 平台 | 文件 |
|------|------|
| Linux x64 | `163music-cli` |
| Windows x64 | `163music-cli.exe` |

无需安装运行时，二进制文件自包含所有依赖。

Linux 下载后需要添加执行权限：

```bash
chmod +x 163music-cli
```

---

## 用法

```
163music-cli <IDs> [选项]
```

**参数：**

| 参数 | 说明 |
|------|------|
| `<IDs>` | 歌曲 / 专辑 / 歌单的 ID 或完整 URL，多个用逗号分隔 |

**选项：**

| 选项 | 说明 | 默认值 |
|------|------|--------|
| `-s, --source` | 音乐来源：`netease` \| `qq` | `netease` |
| `-t, --type` | 搜索类型：`song` \| `album` \| `playlist` | `song` |
| `-f, --format` | 输出格式：`lrc` \| `srt` | `lrc` |
| `-o, --output` | 输出目录 | `.`（当前目录）|
| `-l, --lrc-type` | 歌词排列方式：`stagger` \| `isolated` \| `merged` | `stagger` |
| `-c, --cookie` | 设置 Cookie 并保存到本地配置（下次无需重复传入） | — |

**示例：**

```bash
# 按 ID 下载单曲（网易云）
163music-cli 2055847

# 按 URL 下载
163music-cli "https://music.163.com/song?id=2055847"

# 下载整张专辑，输出为 SRT 格式
163music-cli 34793562 --type album --format srt --output ./lyrics

# 同时下载多首歌
163music-cli 2055847,123456 --output ./lyrics

# 下载 QQ 音乐歌词
163music-cli 123456 --source qq --output ./lyrics

# 首次设置 Cookie（同时执行本次下载，之后无需再传）
163music-cli 2055847 --cookie "MUSIC_U=xxx; __csrf=xxx"
163music-cli 123456 --source qq --cookie "uin=xxx; qm_keyst=xxx"
```

**歌词排列方式说明（`--lrc-type`）：**

- `stagger` — 原文与译文逐行交错排列（默认）
- `isolated` — 原文与译文分别保存为独立文件
- `merged` — 原文与译文合并在同一行

---

## 关键词搜索（`search` 子命令）

通过关键词搜索，然后用交互式菜单（上下键 + 回车）选择目标：

```bash
163music-cli search <关键词> [选项]
```

| 选项 | 说明 | 默认值 |
|------|------|--------|
| `-s, --source` | 音乐来源 | `netease` |
| `-t, --type` | 搜索类型：`song` \| `album` \| `playlist` | `song` |
| `-o, --output` | 选中后直接下载到此目录 | 仅打印 ID |
| `--pick N` | 直接选第 N 条，不显示菜单（适合脚本） | — |

```bash
# 交互式：上下键选择，回车确认
163music-cli search "晴天"

# 搜索并直接下载选中结果
163music-cli search "晴天" --output ./lyrics

# 非交互：取第 1 条结果，仅打印 ID
163music-cli search "晴天" --pick 1

# 非交互：取第 2 条结果并下载
163music-cli search "晴天" --pick 2 --output ./lyrics
```

---

## Cookie 说明

部分歌词需要登录才能获取。`--cookie` 会根据 `--source` 自动区分来源：

- `--source netease`（默认）→ 保存到网易云 Cookie
- `--source qq` → 保存到 QQ 音乐 Cookie

Cookie 保存在 `~/.config/MusicLyricApp/MusicLyricAppSetting.json`，下次运行时自动读取，无需重复传入。

---

## 从源码构建

需要 [.NET 9 SDK](https://dotnet.microsoft.com/download) 或更高版本。

```bash
git clone https://github.com/liu246542/163MusicLyrics-CLI.git
cd 163MusicLyrics-CLI/cross-platform

dotnet build MusicLyricApp.CLI/MusicLyricApp.CLI.csproj -o ./build
dotnet ./build/163music-cli.dll --help
```

发布为单文件可执行程序：

```bash
dotnet publish MusicLyricApp.CLI/MusicLyricApp.CLI.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

---

## 参考

核心 API 实现来自 [jitwxs/163MusicLyrics](https://github.com/jitwxs/163MusicLyrics)，原项目参考了：

- https://github.com/Binaryify/NeteaseCloudMusicApi
- https://github.com/Rain120/qq-music-api
- https://github.com/jsososo/QQMusicApi

## 许可证

[Apache 2.0](https://opensource.org/licenses/Apache-2.0)
