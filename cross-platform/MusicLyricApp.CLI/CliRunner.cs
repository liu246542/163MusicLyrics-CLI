using System;
using System.Collections.Generic;
using System.IO;
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
        string outputDir)
    {
        // 1. 构建 SettingBean（使用默认值，后续可扩展为从 config 加载）
        var setting = new SettingBean();
        setting.Param.SearchSource = source;
        setting.Param.OutputFileFormat = format;
        setting.Param.ShowLrcType = lrcType;

        // 2. 构建 SearchService
        var searchService = new SearchService(setting);

        // 3. 展开 IDs（song 直接用，album/playlist 需查询展开）
        var inputSongIds = new List<InputSongId>();
        foreach (var rawId in ids)
        {
            try
            {
                // CheckInputId 解析 ID 或 URL，返回含 QueryId 的对象（SongId 未设置）
                var parsed = GlobalUtils.CheckInputId(rawId.Trim(), source, type);
                var musicApi = searchService.GetMusicApi(parsed.SearchSource);

                switch (parsed.SearchType)
                {
                    case SearchTypeEnum.SONG_ID:
                        // 2-arg 构造：SongId = 第一个参数，QueryId 继承自 parsed
                        inputSongIds.Add(new InputSongId(parsed.QueryId, parsed));
                        break;
                    case SearchTypeEnum.ALBUM_ID:
                        var albumVo = musicApi.GetAlbumVo(parsed.QueryId).Assert().Data;
                        foreach (var song in albumVo.SimpleSongVos)
                            inputSongIds.Add(new InputSongId(song.DisplayId, parsed));
                        break;
                    case SearchTypeEnum.PLAYLIST_ID:
                        var playlistVo = musicApi.GetPlaylistVo(parsed.QueryId).Assert().Data;
                        foreach (var song in playlistVo.SimpleSongVos)
                            inputSongIds.Add(new InputSongId(song.DisplayId, parsed));
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

        // 4. 搜索歌词
        Console.WriteLine($"正在下载 {inputSongIds.Count} 首歌的歌词...");
        var results = searchService.SearchSongs(inputSongIds, setting);

        // 5. 保存结果（参照 StorageService.WriteToFile 的逻辑）
        Directory.CreateDirectory(outputDir);
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

            // GetOutputContent 返回 List<string>：每个元素是一个完整文件的内容字符串
            // isSingle=true 时只有 1 个文件；SeparateFileForIsolated=true 时可能有多个
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
                var filePath = Path.Combine(outputDir, fileName);
                await File.WriteAllTextAsync(filePath, res[i], encoding);
                Console.WriteLine($"[OK] {fileName}");
            }

            successCount++;
        }

        Console.WriteLine($"\n完成：{successCount}/{results.Count} 首歌词已保存到 {Path.GetFullPath(outputDir)}");
        return successCount == results.Count ? 0 : 1;
    }
}
