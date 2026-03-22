using System.CommandLine;
using MusicLyricApp.CLI;
using MusicLyricApp.Models;

// ── 公共 option 工厂（download 和 search 子命令共用） ──────────────────────

Option<string> MakeSourceOpt() => new(
    aliases: ["--source", "-s"],
    description: "音乐来源: netease (默认) | qq",
    getDefaultValue: () => "netease");

Option<string> MakeTypeOpt() => new(
    aliases: ["--type", "-t"],
    description: "搜索类型: song (默认) | album | playlist",
    getDefaultValue: () => "song");

Option<string> MakeFormatOpt() => new(
    aliases: ["--format", "-f"],
    description: "输出格式: lrc (默认) | srt",
    getDefaultValue: () => "lrc");

Option<string> MakeLrcTypeOpt() => new(
    aliases: ["--lrc-type", "-l"],
    description: "歌词合并方式: stagger (默认) | isolated | merged",
    getDefaultValue: () => "stagger");

// ── 根命令：直接按 ID 下载 ─────────────────────────────────────────────────

var idsArg = new Argument<string[]>(
    name: "ids",
    description: "歌曲/专辑/歌单 ID 或 URL，多个用逗号分隔")
{
    Arity = ArgumentArity.OneOrMore
};

var sourceOpt   = MakeSourceOpt();
var typeOpt     = MakeTypeOpt();
var formatOpt   = MakeFormatOpt();
var lrcTypeOpt  = MakeLrcTypeOpt();

var outputOpt = new Option<string>(
    aliases: ["--output", "-o"],
    description: "输出目录（默认：当前目录）",
    getDefaultValue: () => ".");

var cookieOpt = new Option<string?>(
    aliases: ["--cookie", "-c"],
    description: "设置 Cookie 并保存到本地配置（下次无需重复传入）",
    getDefaultValue: () => null);

var rootCommand = new RootCommand("163MusicLyrics CLI — 下载网易云/QQ音乐歌词");
rootCommand.AddArgument(idsArg);
rootCommand.AddOption(sourceOpt);
rootCommand.AddOption(typeOpt);
rootCommand.AddOption(formatOpt);
rootCommand.AddOption(outputOpt);
rootCommand.AddOption(lrcTypeOpt);
rootCommand.AddOption(cookieOpt);

rootCommand.SetHandler(async (ids, source, type, format, output, lrcType, cookie) =>
{
    var exitCode = await CliRunner.RunAsync(
        ExpandIds(ids),
        ParseSource(source),
        ParseType(type),
        ParseFormat(format),
        ParseLrcType(lrcType),
        output,
        cookie);
    System.Environment.Exit(exitCode);
},
idsArg, sourceOpt, typeOpt, formatOpt, outputOpt, lrcTypeOpt, cookieOpt);

// ── search 子命令：关键词搜索 + 交互式选择 ───────────────────────────────

var keywordArg = new Argument<string>(
    name: "keyword",
    description: "搜索关键词");

var searchSourceOpt  = MakeSourceOpt();
var searchTypeOpt    = MakeTypeOpt();
var searchFormatOpt  = MakeFormatOpt();
var searchLrcTypeOpt = MakeLrcTypeOpt();

var searchOutputOpt = new Option<string?>(
    aliases: ["--output", "-o"],
    description: "选中后直接下载到此目录（不指定则仅打印 ID）",
    getDefaultValue: () => null);

var pickOpt = new Option<int?>(
    name: "--pick",
    description: "直接选第 N 条结果，不显示交互菜单（适合脚本）",
    getDefaultValue: () => null);

var searchCommand = new Command("search", "按关键词搜索，交互式选择后可直接下载");
searchCommand.AddArgument(keywordArg);
searchCommand.AddOption(searchSourceOpt);
searchCommand.AddOption(searchTypeOpt);
searchCommand.AddOption(searchFormatOpt);
searchCommand.AddOption(searchLrcTypeOpt);
searchCommand.AddOption(searchOutputOpt);
searchCommand.AddOption(pickOpt);

searchCommand.SetHandler(async (string keyword, string source, string type, string format, string lrcType, string? output, int? pick) =>
{
    var exitCode = await SearchCommand.RunAsync(
        keyword,
        ParseSource(source),
        ParseType(type),
        pick,
        output,
        ParseFormat(format),
        ParseLrcType(lrcType));
    System.Environment.Exit(exitCode);
},
keywordArg, searchSourceOpt, searchTypeOpt, searchFormatOpt, searchLrcTypeOpt,
searchOutputOpt, pickOpt);

rootCommand.AddCommand(searchCommand);

return await rootCommand.InvokeAsync(args);

// ── 解析辅助 ──────────────────────────────────────────────────────────────

static string[] ExpandIds(string[] ids)
{
    var result = new System.Collections.Generic.List<string>();
    foreach (var id in ids)
        result.AddRange(id.Split(',', System.StringSplitOptions.RemoveEmptyEntries));
    return [..result];
}

static MusicLyricApp.Models.SearchSourceEnum ParseSource(string s) =>
    s.ToLower() == "qq" ? MusicLyricApp.Models.SearchSourceEnum.QQ_MUSIC
                        : MusicLyricApp.Models.SearchSourceEnum.NET_EASE_MUSIC;

static MusicLyricApp.Models.SearchTypeEnum ParseType(string s) => s.ToLower() switch
{
    "album"    => MusicLyricApp.Models.SearchTypeEnum.ALBUM_ID,
    "playlist" => MusicLyricApp.Models.SearchTypeEnum.PLAYLIST_ID,
    _          => MusicLyricApp.Models.SearchTypeEnum.SONG_ID
};

static MusicLyricApp.Models.OutputFormatEnum ParseFormat(string s) =>
    s.ToLower() == "srt" ? MusicLyricApp.Models.OutputFormatEnum.SRT
                         : MusicLyricApp.Models.OutputFormatEnum.LRC;

static MusicLyricApp.Models.ShowLrcTypeEnum ParseLrcType(string s) => s.ToLower() switch
{
    "isolated" => MusicLyricApp.Models.ShowLrcTypeEnum.ISOLATED,
    "merged"   => MusicLyricApp.Models.ShowLrcTypeEnum.MERGE,
    _          => MusicLyricApp.Models.ShowLrcTypeEnum.STAGGER
};
