using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using Spectre.Console;

namespace MusicLyricApp.CLI;

public static class SearchCommand
{
    public static async Task<int> RunAsync(
        string keyword,
        SearchSourceEnum source,
        SearchTypeEnum type,
        int? pick,
        string? outputDir,
        OutputFormatEnum format,
        ShowLrcTypeEnum lrcType,
        string? moveToDir = null)
    {
        var storageService = new StorageService();
        var setting = storageService.ReadAppConfig();

        var musicApi = new SearchService(setting).GetMusicApi(source);
        var result = musicApi.Search(keyword, type);

        if (!result.IsSuccess() || result.Data.IsEmpty())
        {
            AnsiConsole.MarkupLine("[red]没有找到相关结果。[/]");
            return 1;
        }

        var data = result.Data;

        // 构建候选列表（统一成 (displayId, label) 对）
        var candidates = BuildCandidates(data, type);

        if (candidates.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]没有找到相关结果。[/]");
            return 1;
        }

        // 选择：有 TTY → 交互式；无 TTY 或指定 --pick → 直接取
        string selectedId;
        if (pick.HasValue)
        {
            var idx = pick.Value - 1;
            if (idx < 0 || idx >= candidates.Count)
            {
                Console.Error.WriteLine($"[ERROR] --pick 超出范围（共 {candidates.Count} 条结果）");
                return 1;
            }
            selectedId = candidates[idx].Id;
            AnsiConsole.MarkupLine($"已选择: {candidates[idx].Label}");
        }
        else if (Console.IsInputRedirected)
        {
            // 非交互环境：列出结果供参考，提示使用 --pick
            Console.WriteLine($"找到 {candidates.Count} 条结果：");
            for (var i = 0; i < candidates.Count; i++)
                Console.WriteLine($"  {i + 1}. [{candidates[i].Id}] {candidates[i].Label}");
            Console.Error.WriteLine("[ERROR] 非交互环境，请使用 --pick N 指定结果序号");
            return 1;
        }
        else
        {
            // 交互式选择（首项为取消）
            var cancelItem = new Candidate("__cancel__", "[grey][ 取消 ][/]");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<Candidate>()
                    .Title($"[bold]搜索 \"{Markup.Escape(keyword)}\" 的结果[/]（↑↓ 移动，Enter 确认，选「取消」退出）：")
                    .PageSize(12)
                    .UseConverter(c => c.Label)
                    .AddChoices([cancelItem, ..candidates]));
            if (choice.Id == "__cancel__") return 0;
            selectedId = choice.Id;
        }

        // 如果没有指定输出目录，只打印 ID
        if (string.IsNullOrEmpty(outputDir))
        {
            Console.WriteLine(selectedId);
            return 0;
        }

        // 有输出目录 → 直接下载
        return await CliRunner.RunAsync(
            [selectedId], source, type, format, lrcType, outputDir, cookie: null, moveToDir);
    }

    private static List<Candidate> BuildCandidates(SearchResultVo data, SearchTypeEnum type)
    {
        return type switch
        {
            SearchTypeEnum.SONG_ID => data.SongVos.Select(s =>
            {
                var artists = string.Join(", ", s.AuthorName ?? []);
                var duration = TimeSpan.FromMilliseconds(s.Duration);
                var label = $"{Markup.Escape(s.Title)} - {Markup.Escape(artists)}  " +
                            $"[dim]{duration.Minutes:00}:{duration.Seconds:00}  {Markup.Escape(s.AlbumName ?? "")}[/]";
                return new Candidate(s.DisplayId, label);
            }).ToList(),

            SearchTypeEnum.ALBUM_ID => data.AlbumVos.Select(a =>
            {
                var artists = string.Join(", ", a.AuthorName ?? []);
                var label = $"{Markup.Escape(a.AlbumName)} - {Markup.Escape(artists)}  " +
                            $"[dim]{a.SongCount}首  {Markup.Escape(a.PublishTime ?? "")}[/]";
                return new Candidate(a.DisplayId, label);
            }).ToList(),

            SearchTypeEnum.PLAYLIST_ID => data.PlaylistVos.Select(p =>
            {
                var label = $"{Markup.Escape(p.PlaylistName)} - {Markup.Escape(p.AuthorName ?? "")}  " +
                            $"[dim]{p.SongCount}首[/]";
                return new Candidate(p.DisplayId, label);
            }).ToList(),

            _ => []
        };
    }

    private record Candidate(string Id, string Label);
}
