using Sparring.Core;

namespace Sparring.Tests;

public sealed class NeoEBotCatalogTests
{
    [Theory]
    [InlineData("NeoProtossE", StarCraftRace.Protoss, "NeoProtossE.dll", "fast_power_dragoon")]
    [InlineData("NeoTerranE", StarCraftRace.Terran, "NeoTerranE.dll", "factory_expand")]
    [InlineData("NeoZergE", StarCraftRace.Zerg, "NeoZergE.dll", "three_hatch_hydra")]
    public void BundledCatalogIncludesNeoEPracticeOnlyDlls(
        string botName,
        StarCraftRace race,
        string executable,
        string expectedBuildId)
    {
        var repo = FindRepositoryRoot();
        var catalog = PracticeAssetCatalogReader.Read(Path.Combine(repo, "data"));
        var bot = Assert.Single(catalog.Bots, bot => bot.Name == botName);

        Assert.Equal(race, bot.Race);
        Assert.Equal(BotExecutableKind.Dll, bot.ExecutableKind);
        Assert.Equal("4.4.0", bot.BwapiVersion);
        Assert.Null(bot.Elo);
        Assert.True(bot.PracticeOnly);
        Assert.False(PracticeBotCandidatePolicy.IsLadderEligible(bot));
        Assert.False(PracticeBotCandidatePolicy.IsSparringRandomEligible(bot));
        Assert.Equal(executable, bot.ExecutableName);
        Assert.EndsWith(botName, bot.SourceDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.True(bot.HasSelectableBuilds);
        Assert.Contains(bot.AvailableBuildOptions, option => option.Id == expectedBuildId);
        Assert.Contains("E급", bot.Description);

        var dllPath = Path.Combine(bot.SourceDirectory!, executable);
        Assert.True(File.Exists(dllPath), dllPath);
        var bytes = File.ReadAllBytes(dllPath);
        Assert.True(bytes.Length > 100_000);
        Assert.Equal((byte)'M', bytes[0]);
        Assert.Equal((byte)'Z', bytes[1]);
    }

    [Theory]
    [InlineData("NeoProtossE")]
    [InlineData("NeoTerranE")]
    [InlineData("NeoZergE")]
    public void NeoEMapsStayOnVerifiedOneVsOneAndFourPlayerFamilies(string botName)
    {
        var repo = FindRepositoryRoot();
        var catalog = PracticeAssetCatalogReader.Read(Path.Combine(repo, "data"));
        var bot = Assert.Single(catalog.Bots, bot => bot.Name == botName);

        var maps = PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id);
        var names = maps.Select(map => map.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Contains("(4)Fighting Spirit", names);
        Assert.Contains("(4)Fighting Spirit 1.4", names);
        Assert.Contains("(4)Python", names);
        Assert.Contains("(4)Circuit Breaker", names);
        Assert.Contains("(2)Match Point", names);
        Assert.Contains("(4)Polypoid 1.65", names);
        Assert.Contains("(4)Polypoid 1.75", names);
        Assert.All(maps, map => Assert.True(IsNeoMapFamily(map), map.Name));
    }

    private static bool IsNeoMapFamily(PracticeMap map)
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
