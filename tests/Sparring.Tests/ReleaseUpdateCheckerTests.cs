using System.Text;
using Sparring.Core;

namespace Sparring.Tests;

public sealed class ReleaseUpdateCheckerTests
{
    [Fact]
    public async Task ParseLatestFindsSetupAsset()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            {
              "tag_name": "1.5.0",
              "name": "Sparring 1.5.0",
              "html_url": "https://github.com/NeoMindStd/SPArring/releases/tag/1.5.0",
              "assets": [
                {
                  "name": "Sparring-1.5.0-setup.exe",
                  "browser_download_url": "https://example.test/Sparring-1.5.0-setup.exe"
                }
              ]
            }
            """));

        var update = await GitHubReleaseUpdateChecker.ParseLatestAsync(stream);

        Assert.NotNull(update);
        Assert.Equal("1.5.0", update.TagName);
        Assert.Equal("Sparring-1.5.0-setup.exe", update.FindSetupAsset()?.Name);
    }

    [Fact]
    public void CompareVersionsIgnoresOptionalVPrefix()
    {
        Assert.True(GitHubReleaseUpdateChecker.CompareVersions("v1.5.0", "1.4.9") > 0);
        Assert.Equal(0, GitHubReleaseUpdateChecker.CompareVersions("1.4", "1.4.0"));
        Assert.True(GitHubReleaseUpdateChecker.CompareVersions("1.4.0", "1.4.1") < 0);
    }
}
