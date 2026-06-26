using Sparring.Core;

namespace Sparring.Tests;

public sealed class NeoProtossBotCatalogTests
{
    [Fact]
    public void BundledCatalogIncludesNeoProtossFBwapiDll()
    {
        var repo = FindRepositoryRoot();
        var catalog = PracticeAssetCatalogReader.Read(Path.Combine(repo, "data"));

        Assert.True(catalog.Bots.Count >= 80);
        Assert.Contains(catalog.Bots, bot => bot.Name == "Dragon");

        var bot = Assert.Single(catalog.Bots, bot => bot.Name == "NeoProtossF");

        Assert.Equal(StarCraftRace.Protoss, bot.Race);
        Assert.Equal(BotExecutableKind.Dll, bot.ExecutableKind);
        Assert.Equal("4.4.0", bot.BwapiVersion);
        Assert.Null(bot.Elo);
        Assert.True(bot.PracticeOnly);
        Assert.False(PracticeBotCandidatePolicy.IsLadderEligible(bot));
        Assert.False(PracticeBotCandidatePolicy.IsSparringRandomEligible(bot));
        Assert.Contains("개발 중", bot.Description);
        Assert.Contains("어색", bot.Description);
        Assert.EndsWith("NeoProtossF", bot.SourceDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.True(bot.HasSelectableBuilds);
        Assert.Contains(bot.AvailableBuildOptions, option => option.Id == "1012");
        Assert.Contains(bot.AvailableBuildOptions, option => option.Id == "fast_power_dragoon");
        Assert.Contains(bot.AvailableBuildOptions, option => option.Id == "23_nexus");
        Assert.Contains(bot.AvailableBuildOptions, option => option.Matchups.Contains("vs Terran", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(bot.AvailableBuildOptions, option => option.Matchups.Contains("vs Zerg", StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(Path.Combine(bot.SourceDirectory!, "NeoProtossF.dll")));

        var bytes = File.ReadAllBytes(Path.Combine(bot.SourceDirectory!, "NeoProtossF.dll"));
        Assert.True(bytes.Length > 100_000);
        Assert.Equal((byte)'M', bytes[0]);
        Assert.Equal((byte)'Z', bytes[1]);
    }

    [Fact]
    public void NeoProtossFMapsStayOnVerifiedOneVsOneAndFourPlayerFamilies()
    {
        var repo = FindRepositoryRoot();
        var catalog = PracticeAssetCatalogReader.Read(Path.Combine(repo, "data"));
        var bot = Assert.Single(catalog.Bots, bot => bot.Name == "NeoProtossF");

        var maps = PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id);
        var names = maps.Select(map => map.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Contains("(4)Fighting Spirit", names);
        Assert.Contains("(4)Fighting Spirit 1.4", names);
        Assert.Contains("(4)Python", names);
        Assert.Contains("(4)Circuit Breaker", names);
        Assert.Contains("(2)Match Point", names);
        Assert.Contains("(4)Polypoid 1.65", names);
        Assert.Contains("(4)Polypoid 1.75", names);
        Assert.All(maps, map => Assert.True(IsNeoProtossFMapFamily(map), map.Name));
    }

    private static bool IsNeoProtossFMapFamily(PracticeMap map)
    {
        var text = $"{map.Name} {map.FileName}";
        return text.Contains("Fighting Spirit", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Polypoid", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Python", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Circuit Breaker", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Match", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sparring.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "data")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Sparring repository root.");
    }
}
