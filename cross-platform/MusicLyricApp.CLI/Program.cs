using System.CommandLine;
using MusicLyricApp.CLI;
using MusicLyricApp.Models;

var idsArg = new Argument<string[]>(
    name: "ids",
    description: "歌曲/专辑/歌单 ID 或 URL，多个用逗号分隔")
{
    Arity = ArgumentArity.OneOrMore
};

var sourceOpt = new Option<string>(
    aliases: ["--source", "-s"],
    description: "音乐来源: netease (默认) | qq",
    getDefaultValue: () => "netease");

var typeOpt = new Option<string>(
    aliases: ["--type", "-t"],
    description: "搜索类型: song (默认) | album | playlist",
    getDefaultValue: () => "song");

var formatOpt = new Option<string>(
    aliases: ["--format", "-f"],
    description: "输出格式: lrc (默认) | srt",
    getDefaultValue: () => "lrc");

var outputOpt = new Option<string>(
    aliases: ["--output", "-o"],
    description: "输出目录（默认：当前目录）",
    getDefaultValue: () => ".");

var lrcTypeOpt = new Option<string>(
    aliases: ["--lrc-type", "-l"],
    description: "歌词合并方式: stagger (默认) | isolated | merged",
    getDefaultValue: () => "stagger");

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
    var searchSource = source.ToLower() switch
    {
        "qq" => SearchSourceEnum.QQ_MUSIC,
        _ => SearchSourceEnum.NET_EASE_MUSIC
    };

    var searchType = type.ToLower() switch
    {
        "album" => SearchTypeEnum.ALBUM_ID,
        "playlist" => SearchTypeEnum.PLAYLIST_ID,
        _ => SearchTypeEnum.SONG_ID
    };

    var outputFormat = format.ToLower() switch
    {
        "srt" => OutputFormatEnum.SRT,
        _ => OutputFormatEnum.LRC
    };

    var lrcTypeEnum = lrcType.ToLower() switch
    {
        "isolated" => ShowLrcTypeEnum.ISOLATED,
        "merged" => ShowLrcTypeEnum.MERGE,
        _ => ShowLrcTypeEnum.STAGGER
    };

    // 支持逗号分隔的多 ID（也支持 shell 传多参数）
    var allIds = new System.Collections.Generic.List<string>();
    foreach (var id in ids)
        allIds.AddRange(id.Split(',', System.StringSplitOptions.RemoveEmptyEntries));

    var exitCode = await CliRunner.RunAsync(
        [..allIds], searchSource, searchType, outputFormat, lrcTypeEnum, output, cookie);

    System.Environment.Exit(exitCode);
},
idsArg, sourceOpt, typeOpt, formatOpt, outputOpt, lrcTypeOpt, cookieOpt);

return await rootCommand.InvokeAsync(args);
