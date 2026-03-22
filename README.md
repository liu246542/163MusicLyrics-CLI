# 163MusicLyrics CLI

A command-line tool for downloading lyrics from NetEase Cloud Music and QQ Music.

> Forked from [jitwxs/163MusicLyrics](https://github.com/jitwxs/163MusicLyrics). This fork strips the GUI and exposes the core functionality as a CLI.

[中文说明 README_ZH.md](./README_ZH.md)

[![Release](https://img.shields.io/github/v/release/liu246542/163MusicLyrics-CLI.svg)](https://github.com/liu246542/163MusicLyrics-CLI/releases)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

---

## Demo

![CLI demo](images/cli_example.gif)

The demo above shows the full `search` + `--move-to` workflow:

1. Search for an album by keyword (`--type album`) — an interactive menu appears with results
2. Select the target album with arrow keys and Enter
3. Lyrics are downloaded to `./lyrics/` (an artist-album subfolder is created automatically)
4. `--move-to` scans the music directory, matches each LRC file to its audio counterpart using fuzzy similarity (supports Traditional/Simplified Chinese), and presents a confirmation table
5. On confirmation, LRC files are moved and renamed to match the audio filename stem

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
| `-c, --cookie` | Set Cookie and save it locally (no need to repeat next time) | — |
| `--move-to` | After downloading, match and move LRC files into this music directory (auto-renamed to audio filename stem) | — |

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

# Set Cookie (saved locally, no need to repeat on future runs)
163music-cli 2055847 --cookie "your_netease_cookie_here"
163music-cli 123456 --source qq --cookie "your_qq_cookie_here"

# Download an album and move matched LRC files into the music directory
163music-cli 34793562 --type album --output ./tmp-lyrics --move-to /music/Jay/
```

**Lyric layout modes** (`--lrc-type`):

- `stagger` — Original and translation interleaved line by line (default)
- `isolated` — Original and translation in separate files
- `merged` — Original and translation merged on the same line

---

## Keyword Search (`search` subcommand)

Use the `search` subcommand to find songs by keyword with an interactive selection menu:

```bash
163music-cli search "keyword" [options]
```

| Option | Description | Default |
|--------|-------------|---------|
| `-s, --source` | Music provider | `netease` |
| `-t, --type` | Search type: `song` \| `album` \| `playlist` | `song` |
| `-o, --output` | Download selected result to this directory | *(print ID only)* |
| `--pick N` | Select Nth result without interactive menu (for scripts) | — |
| `--move-to` | After downloading, match and move LRC files into this music directory | — |

```bash
# Interactive: arrow keys to navigate, Enter to confirm, select ↩ 取消 to exit
163music-cli search "晴天"

# Search and download the selected result directly
163music-cli search "晴天" --output ./lyrics

# Non-interactive: pick result #1 and print its ID
163music-cli search "晴天" --pick 1

# Non-interactive: pick result #2 and download it
163music-cli search "晴天" --pick 2 --output ./lyrics
```

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

- **Cookies:** Some lyrics require authentication. Pass your cookie with `--cookie` on first use — it will be saved to `~/.config/MusicLyricApp/MusicLyricAppSetting.json` and reused automatically on subsequent runs.
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
