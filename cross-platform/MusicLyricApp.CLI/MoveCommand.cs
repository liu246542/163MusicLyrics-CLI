using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

namespace MusicLyricApp.CLI;

public static class MoveCommand
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".flac", ".m4a", ".mp3", ".wav", ".aac", ".ape", ".opus"
    };

    public static async Task<int> RunAsync(
        IReadOnlyList<string> writtenLrcPaths,
        string moveToDir)
    {
        if (writtenLrcPaths.Count == 0)
        {
            Console.WriteLine("[INFO] 没有已下载的 LRC 文件，跳过移动步骤。");
            return 0;
        }

        if (!Directory.Exists(moveToDir))
        {
            Console.Error.WriteLine($"[ERROR] --move-to 目录不存在: {moveToDir}");
            return 1;
        }

        // 扫描目标目录的音频文件
        var audioFiles = Directory.GetFiles(moveToDir)
            .Where(f => AudioExtensions.Contains(Path.GetExtension(f)))
            .ToList();

        if (audioFiles.Count == 0)
        {
            Console.Error.WriteLine($"[WARN] {moveToDir} 中没有找到音频文件，跳过移动步骤。");
            return 0;
        }

        // 为每个 LRC 文件找最佳匹配音频
        var matches = new List<(string LrcPath, string? AudioPath, double Score)>();

        foreach (var lrcPath in writtenLrcPaths)
        {
            var lrcTitle = ExtractLrcTitle(Path.GetFileNameWithoutExtension(lrcPath));

            string? bestAudio = null;
            double bestScore = 0;

            foreach (var audioPath in audioFiles)
            {
                var audioTitle = ExtractAudioTitle(Path.GetFileNameWithoutExtension(audioPath));
                var score = Similarity(lrcTitle, audioTitle);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAudio = audioPath;
                }
            }

            var threshold = bestAudio is null
                ? 0.6
                : AdaptiveThreshold(lrcTitle, ExtractAudioTitle(Path.GetFileNameWithoutExtension(bestAudio)));

            matches.Add((lrcPath, bestScore >= threshold ? bestAudio : null, bestScore));
        }

        // Spectre.Console 表格展示匹配结果
        var table = new Table();
        table.AddColumn("#");
        table.AddColumn("LRC 文件");
        table.AddColumn("匹配音频");
        table.AddColumn("相似度");

        for (var i = 0; i < matches.Count; i++)
        {
            var (lrcPath, audioPath, score) = matches[i];
            var lrcFile = Markup.Escape(Path.GetFileName(lrcPath));
            var audioFile = audioPath is null
                ? "[red]未找到匹配[/]"
                : Markup.Escape(Path.GetFileName(audioPath));
            var scoreStr = audioPath is null ? "[red]-[/]" : $"[green]{score:P0}[/]";
            table.AddRow((i + 1).ToString(), lrcFile, audioFile, scoreStr);
        }

        AnsiConsole.Write(table);

        var validMatches = matches.Where(m => m.AudioPath is not null).ToList();
        if (validMatches.Count == 0)
        {
            Console.WriteLine("没有找到任何匹配，跳过移动步骤。");
            return 0;
        }

        // 非交互环境跳过确认
        if (Console.IsInputRedirected)
        {
            Console.WriteLine("[WARN] 非交互环境，跳过文件移动步骤。请在交互终端中运行以执行移动。");
            return 0;
        }

        if (!AnsiConsole.Confirm($"将 {validMatches.Count} 个 LRC 文件移动并重命名到 {moveToDir}？", defaultValue: false))
        {
            Console.WriteLine("已取消。");
            return 0;
        }

        // 执行移动：重命名为音频文件的 stem + .lrc
        foreach (var (lrcPath, audioPath, _) in validMatches)
        {
            var audioStem = Path.GetFileNameWithoutExtension(audioPath!);
            var destFileName = $"{audioStem}.lrc";
            var destPath = Path.Combine(moveToDir, destFileName);

            if (File.Exists(destPath))
            {
                Console.WriteLine($"[WARN] 目标已存在，跳过: {destFileName}");
                continue;
            }

            File.Move(lrcPath, destPath);
            Console.WriteLine($"[OK] {Path.GetFileName(lrcPath)} → {destFileName}");
        }

        return 0;
    }

    // 从 LRC 文件名提取歌曲名：取第一个 " - " 之前的部分
    private static string ExtractLrcTitle(string stem)
    {
        var idx = stem.IndexOf(" - ", StringComparison.Ordinal);
        return idx >= 0 ? stem[..idx].Trim() : stem.Trim();
    }

    // 从音频文件名提取歌曲名：去掉开头数字/序号前缀，去掉末尾括号内容
    private static string ExtractAudioTitle(string stem)
    {
        // 去掉开头形如 "01 - "、"1."、"01. " 等序号前缀
        var s = Regex.Replace(stem, @"^\d+[\.\s\-]+\s*", "").Trim();
        // 去掉末尾括号内容，如 " (Live)"、" [Remastered]"
        s = Regex.Replace(s, @"\s*[\(\[][^\)\]]*[\)\]]\s*$", "").Trim();
        return s;
    }

    private static string Normalize(string s) =>
        s.ToLowerInvariant().Replace(" ", "");

    private static double Similarity(string a, string b)
    {
        var na = Normalize(a);
        var nb = Normalize(b);

        if (na == nb) return 1.0;

        // 子串包含检测：修复短名称（如"情愿"）匹配问题
        var (shorter, longer) = na.Length <= nb.Length ? (na, nb) : (nb, na);
        if (shorter.Length >= 2 && longer.Contains(shorter, StringComparison.Ordinal))
            return 0.9;

        // LCS 相似度（O(n) 空间滚动 DP）
        var lcs = LcsLength(na, nb);
        return (double)(2 * lcs) / (na.Length + nb.Length);
    }

    private static double AdaptiveThreshold(string a, string b)
    {
        var minLen = Math.Min(Normalize(a).Length, Normalize(b).Length);
        return minLen <= 2 ? 0.5 : 0.6;
    }

    private static int LcsLength(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                curr[j] = a[i - 1] == b[j - 1]
                    ? prev[j - 1] + 1
                    : Math.Max(prev[j], curr[j - 1]);
            }
            Array.Copy(curr, prev, curr.Length);
            Array.Clear(curr, 0, curr.Length);
        }

        return prev[b.Length];
    }
}
