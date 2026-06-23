using Sparring.Core;

namespace Sparring.Tests;

public sealed class BotProfileSummarizerTests
{
    [Fact]
    public void CleanDescriptionRemovesUploadNoiseUrlsAndMetadata()
    {
        var cleaned = BotProfileSummarizer.CleanDescription(
            "(Test upload by schnail admin)AIIDE 2019 Version.\r\nhttps://example.com/bot\r\nBASIL:PUBLISH-READ");

        Assert.Equal("AIIDE 2019 Version.", cleaned);
    }

    [Fact]
    public void SummarizeRecognizesExactRushBuilds()
    {
        var bot = Bot("Yuanheng Zhu", StarCraftRace.Protoss, "Probe rush");

        var summary = BotProfileSummarizer.Summarize(bot);

        Assert.Equal("초반 찌르기 지향", summary.StyleLabel);
        Assert.Equal("프로브 러시", summary.BuildLabel);
    }

    [Fact]
    public void SummarizeFallsBackToReadableRaceDefaultWhenDescriptionIsEmpty()
    {
        var bot = Bot("Generic", StarCraftRace.Terran, "(Test upload by schnail admin)");

        var summary = BotProfileSummarizer.Summarize(bot);

        Assert.Equal("테란 기본 운영", summary.BuildLabel);
        Assert.Contains("기본 운영형", summary.PlayerSummary);
    }

    [Fact]
    public void SummarizeReplacesEnglishOnlyDescriptionWithKoreanPlayerSummary()
    {
        var bot = Bot("CarrierBot", StarCraftRace.Protoss, "Reactive protoss with carrier. Based on old bot notes.");

        var summary = BotProfileSummarizer.Summarize(bot);

        Assert.Equal("후반 운영 지향", summary.StyleLabel);
        Assert.Equal("캐리어 테크 압박", summary.BuildLabel);
        Assert.Contains("캐리어 테크 압박", summary.PlayerSummary);
        Assert.DoesNotContain("Reactive protoss", summary.PlayerSummary);
    }

    private static PracticeBot Bot(string name, StarCraftRace race, string? description)
    {
        return new PracticeBot(
            Guid.NewGuid(),
            name,
            race,
            $"{name}.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            PracticeOnly: false,
            SupportedMapIds: new HashSet<Guid>(),
            Description: description,
            Author: null,
            SourceDirectory: null);
    }
}
