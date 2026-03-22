using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.CLI;

public static class CliRunner
{
    public static async Task<int> RunAsync(
        string[] ids,
        SearchSourceEnum source,
        SearchTypeEnum type,
        OutputFormatEnum format,
        ShowLrcTypeEnum lrcType,
        string outputDir,
        string? cookie)
    {
        // 1. 加载已保存的配置（包含 Cookie、代理等），不存在则使用默认值
        var storageService = new StorageService();
        var setting = storageService.ReadAppConfig();

        // 2. 覆盖本次运行的搜索参数
        setting.Param.SearchSource = source;
        setting.Param.OutputFileFormat = format;
        setting.Param.ShowLrcType = lrcType;

        // 3. 如果传入了 Cookie，保存到对应来源并持久化
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            if (source == SearchSourceEnum.QQ_MUSIC)
                setting.Config.QQMusicCookie = cookie;
            else
                setting.Config.NetEaseCookie = cookie;

            storageService.SaveConfig(setting);
            Console.WriteLine($"[INFO] Cookie 已保存到 {Constants.GetConfigFilePath()}");
        }

        // 4. 构建 SearchService
        var searchService = new SearchService(setting);

        // 5. 展开 IDs，同时记录每首歌对应的输出目录
        //    专辑 → 自动创建 "歌手 - 专辑名" 子目录
        //    单曲/歌单 → 使用 outputDir
        var inputSongIds = new List<InputSongId>();
        var songTargetDirs = new Dictionary<string, string>(); // songId → targetDir

        foreach (var rawId in ids)
        {
            try
            {
                var parsed = GlobalUtils.CheckInputId(rawId.Trim(), source, type);
                var musicApi = searchService.GetMusicApi(parsed.SearchSource);

                switch (parsed.SearchType)
                {
                    case SearchTypeEnum.SONG_ID:
                        var songInput = new InputSongId(parsed.QueryId, parsed);
                        inputSongIds.Add(songInput);
                        songTargetDirs[parsed.QueryId] = outputDir;
                        break;

                    case SearchTypeEnum.ALBUM_ID:
                        var albumVo = musicApi.GetAlbumVo(parsed.QueryId).Assert().Data;
                        var albumDir = Path.Combine(outputDir, BuildAlbumFolderName(albumVo));
                        foreach (var song in albumVo.SimpleSongVos)
                        {
                            inputSongIds.Add(new InputSongId(song.DisplayId, parsed));
                            songTargetDirs[song.DisplayId] = albumDir;
                        }
                        Console.WriteLine($"[INFO] 专辑将保存到：{albumDir}");
                        break;

                    case SearchTypeEnum.PLAYLIST_ID:
                        var playlistVo = musicApi.GetPlaylistVo(parsed.QueryId).Assert().Data;
                        foreach (var song in playlistVo.SimpleSongVos)
                        {
                            inputSongIds.Add(new InputSongId(song.DisplayId, parsed));
                            songTargetDirs[song.DisplayId] = outputDir;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] 解析 ID 失败: {rawId} — {ex.Message}");
                return 1;
            }
        }

        if (inputSongIds.Count == 0)
        {
            Console.Error.WriteLine("[ERROR] 没有有效的歌曲 ID");
            return 1;
        }

        // 6. 搜索歌词
        Console.WriteLine($"正在下载 {inputSongIds.Count} 首歌的歌词...");
        var results = searchService.SearchSongs(inputSongIds, setting);

        // 7. 保存结果
        var ext = setting.Param.OutputFileFormat.ToDescription().ToLower();
        var encoding = GlobalUtils.GetEncoding(setting.Param.Encoding);
        var successCount = 0;

        foreach (var (songId, resultVo) in results)
        {
            if (!resultVo.IsSuccess())
            {
                Console.Error.WriteLine($"[WARN] 歌曲 {songId} 下载失败: {resultVo.ErrorMsg}");
                continue;
            }

            var saveVo = resultVo.Data;

            if (saveVo.LyricVo.IsEmpty())
            {
                Console.Error.WriteLine($"[WARN] 歌曲 {songId}（{saveVo.SongVo.Name}）暂无歌词");
                continue;
            }

            var targetDir = songTargetDirs.TryGetValue(songId, out var d) ? d : outputDir;
            Directory.CreateDirectory(targetDir);

            var res = await LyricUtils.GetOutputContent(saveVo.LyricVo, setting);
            var baseName = GlobalUtils.GetOutputName(
                saveVo,
                setting.Config.OutputFileNameFormat,
                setting.Config.SingerSeparator,
                setting.Config.SingerCountLimit);
            var isSingle = res.Count == 1;

            for (var i = 0; i < res.Count; i++)
            {
                var fileName = isSingle ? $"{baseName}.{ext}" : $"{baseName}-{i}.{ext}";
                var filePath = Path.Combine(targetDir, fileName);
                await File.WriteAllTextAsync(filePath, res[i], encoding);
                Console.WriteLine($"[OK] {fileName}");
            }

            successCount++;
        }

        // 汇总信息：按目录分组展示
        var dirs = songTargetDirs.Values.Distinct().ToList();
        var savedPath = dirs.Count == 1 ? Path.GetFullPath(dirs[0]) : Path.GetFullPath(outputDir);
        Console.WriteLine($"\n完成：{successCount}/{results.Count} 首歌词已保存到 {savedPath}");
        return successCount == results.Count ? 0 : 1;
    }

    private static string BuildAlbumFolderName(AlbumVo albumVo)
    {
        var artist = albumVo.SimpleSongVos?.FirstOrDefault()?.Singer?.FirstOrDefault() ?? "Unknown";
        return Sanitize($"{artist} - {albumVo.Name}");
    }

    internal static string Sanitize(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
    }
}
