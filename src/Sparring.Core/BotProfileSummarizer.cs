using System.Text.RegularExpressions;

namespace Sparring.Core;

public sealed record BotProfileSummary(
    string StyleLabel,
    string BuildLabel,
    string PlayerSummary);

public static partial class BotProfileSummarizer
{
    public static BotProfileSummary Summarize(PracticeBot bot)
    {
        var source = CleanDescription(bot.Description);
        var searchText = string.Join(' ', bot.Name, bot.ExecutableName, source).ToLowerInvariant();
        var style = InferStyle(searchText);
        var build = InferBuild(searchText, bot.Race);
        var summary = string.IsNullOrWhiteSpace(source)
            ? BuildKoreanSummary(bot.Race, style, build, hasOriginalDescription: false)
            : IsMostlyAscii(source)
                ? BuildKoreanSummary(bot.Race, style, build, hasOriginalDescription: true)
            : source;

        return new BotProfileSummary(style, build, summary);
    }

    public static string CleanDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var text = description
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<hr/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("&apos;", "'", StringComparison.OrdinalIgnoreCase);
        text = TestUploadRegex().Replace(text, string.Empty);
        text = UrlRegex().Replace(text, string.Empty);
        text = MetadataLineRegex().Replace(text, string.Empty);
        text = SeparatorRegex().Replace(text, " ");
        text = WhitespaceRegex().Replace(text, " ").Trim();

        return text.Length > 240
            ? text[..237].TrimEnd() + "..."
            : text;
    }

    private static string InferStyle(string text)
    {
        if (ContainsAny(text, "micro", "mutalisk micro", "muta micro", "potential fields"))
        {
            return "마이크로컨트롤 지향";
        }

        if (ContainsAny(text, "rush", "rushing", "probe", "zealot", "marine hell", "early", "cheese"))
        {
            return "초반 찌르기 지향";
        }

        if (ContainsAny(text, "carrier", "macro", "late", "defensive", "turtle"))
        {
            return "후반 운영 지향";
        }

        if (ContainsAny(text, "adaptive", "counter-strategy", "counter strategy", "recognizing", "many builds", "mix of units", "strategy"))
        {
            return "상황 대응형";
        }

        return "균형형 운영";
    }

    private static string InferBuild(string text, StarCraftRace race)
    {
        if (ContainsAny(text, "probe rush"))
        {
            return "프로브 러시";
        }

        if (ContainsAny(text, "zealot-rush", "zealot rush", "zealot-rushing", "zealot rushing"))
        {
            return "질럿 러시";
        }

        if (ContainsAny(text, "marine hell", "marine"))
        {
            return "마린 중심 초반 압박";
        }

        if (ContainsAny(text, "carrier"))
        {
            return "캐리어 테크 압박";
        }

        if (ContainsAny(text, "ling", "hydra", "muta"))
        {
            return "저글링/히드라/뮤탈 혼합";
        }

        if (ContainsAny(text, "bio terran", "bio"))
        {
            return "바이오닉 운영";
        }

        if (ContainsAny(text, "tank", "siege", "mech"))
        {
            return "메카닉 운영";
        }

        if (ContainsAny(text, "wall-in", "wall in"))
        {
            return "입구 막기 기반 운영";
        }

        if (ContainsAny(text, "many builds", "build orders", "dynamic build", "counter-strategy", "counter strategy"))
        {
            return "다양한 빌드/상황 대응";
        }

        return $"{RaceName(race)} 기본 운영";
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildKoreanSummary(
        StarCraftRace race,
        string style,
        string build,
        bool hasOriginalDescription)
    {
        if (!hasOriginalDescription && build.Equals($"{RaceName(race)} 기본 운영", StringComparison.Ordinal))
        {
            return $"{RaceName(race)} 기본 운영형 봇입니다. 원문 빌드 설명이 부족해 카탈로그 정보와 이름 단서를 기준으로 요약했습니다.";
        }

        var sourceNote = hasOriginalDescription
            ? "영문 설명을 바탕으로 플레이 성향을 요약했습니다."
            : "원문 빌드 설명이 부족해 카탈로그 정보와 이름 단서를 기준으로 요약했습니다.";
        return $"{RaceName(race)} {style} 봇입니다. 주로 {build} 흐름을 예상하면 됩니다. {sourceNote}";
    }

    private static bool IsMostlyAscii(string text)
    {
        var letters = text.Where(char.IsLetter).ToList();
        if (letters.Count == 0)
        {
            return true;
        }

        var asciiLetters = letters.Count(letter => letter <= '\u007f');
        return asciiLetters / (double)letters.Count >= 0.8;
    }

    private static string RaceName(StarCraftRace race)
    {
        return race switch
        {
            StarCraftRace.Terran => "테란",
            StarCraftRace.Protoss => "프로토스",
            StarCraftRace.Zerg => "저그",
            StarCraftRace.Random => "랜덤",
            _ => "범용"
        };
    }

    [GeneratedRegex(@"\(Test upload by schnail admin\)|\(SCHNAIL test upload\)", RegexOptions.IgnoreCase)]
    private static partial Regex TestUploadRegex();

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b(BASIL|MAP-POOL)\s*:[^\n]+", RegexOptions.IgnoreCase)]
    private static partial Regex MetadataLineRegex();

    [GeneratedRegex(@"[=\-]{4,}")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
