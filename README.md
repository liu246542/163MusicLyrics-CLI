# 163MusicLyrics CLI

A command-line tool for downloading lyrics from NetEase Cloud Music and QQ Music.

> Forked from [jitwxs/163MusicLyrics](https://github.com/jitwxs/163MusicLyrics). This fork strips the GUI and exposes the core functionality as a CLI.

[![Release](https://img.shields.io/github/v/release/liu246542/163MusicLyrics-CLI.svg)](https://github.com/liu246542/163MusicLyrics-CLI/releases)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

---

## Installation

Download the latest binary from [Releases](https://github.com/liu246542/163MusicLyrics-CLI/releases):

| Platform | File |
|----------|------|
| Linux x64 | `163music-cli` |
| Windows x64 | `163music-cli.exe` |

No runtime installation required — binaries are self-contained.

---

## Usage

```
163music-cli <IDs> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<IDs>` | Song / Album / Playlist ID or URL. Multiple values separated by commas. |

**Options:**

| Option | Description | Default |
|--------|-------------|---------|
| `-s, --source` | Music provider: `netease` \| `qq` | `netease` |
| `-t, --type` | Search type: `song` \| `album` \| `playlist` | `song` |
| `-f, --format` | Output format: `lrc` \| `srt` | `lrc` |
| `-o, --output` | Output directory | `.` (current dir) |
| `-l, --lrc-type` | Lyric layout: `stagger` \| `isolated` \| `merged` | `stagger` |

**Examples:**

```bash
# Download a single song by ID (NetEase)
163music-cli 2055847

# Download by URL
163music-cli "https://music.163.com/song?id=2055847"

# Download an entire album, save as SRT
163music-cli 34793562 --type album --format srt --output ./lyrics

# Download multiple songs at once
163music-cli 2055847,123456 --output ./lyrics

# Download from QQ Music
163music-cli 123456 --source qq --output ./lyrics
```

**Lyric layout modes** (`--lrc-type`):

- `stagger` — Original and translation interleaved line by line (default)
- `isolated` — Original and translation in separate files
- `merged` — Original and translation merged on the same line

---

## Build from Source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download) or later.

```bash
git clone https://github.com/liu246542/163MusicLyrics-CLI.git
cd 163MusicLyrics-CLI/cross-platform

dotnet build MusicLyricApp.CLI/MusicLyricApp.CLI.csproj -o ./build
dotnet ./build/163music-cli.dll --help
```

To publish a self-contained single-file binary:

```bash
dotnet publish MusicLyricApp.CLI/MusicLyricApp.CLI.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

---

## Notes

- **Cookies:** Some lyrics require authentication. If downloads fail, set your cookie via the environment:
  ```bash
  # Not yet implemented — workaround: edit CliRunner.cs and set
  # setting.Config.NetEaseCookie or setting.Config.QQMusicCookie directly.
  ```
- Output filenames follow the pattern `{song name} - {artist}.{ext}`.
- Instrumental tracks with no lyrics will be skipped with a warning.

---

## Reference

Core API implementations are based on [jitwxs/163MusicLyrics](https://github.com/jitwxs/163MusicLyrics), which in turn references:

- https://github.com/Binaryify/NeteaseCloudMusicApi
- https://github.com/Rain120/qq-music-api
- https://github.com/jsososo/QQMusicApi

## License

[Apache 2.0](https://opensource.org/licenses/Apache-2.0)
