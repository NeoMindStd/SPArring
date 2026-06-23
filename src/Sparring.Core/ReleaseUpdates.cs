using System.Net.Http.Headers;
using System.Text.Json;

namespace Sparring.Core;

public sealed record ReleaseAssetInfo(string Name, Uri DownloadUrl);

public sealed record ReleaseUpdateInfo(
    string TagName,
    string Title,
    Uri HtmlUrl,
    IReadOnlyList<ReleaseAssetInfo> Assets)
{
    public ReleaseAssetInfo? FindSetupAsset()
    {
        return Assets.FirstOrDefault(asset =>
            asset.Name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase) &&
            asset.Name.Contains("Sparring", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class GitHubReleaseUpdateChecker
{
    public const string LatestReleaseApi = "https://api.github.com/repos/NeoMindStd/SPArring/releases/latest";

    private readonly HttpClient _client;

    public GitHubReleaseUpdateChecker(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
        if (!_client.DefaultRequestHeaders.UserAgent.Any())
        {
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Sparring", "1.0"));
        }
    }

    public async Task<ReleaseUpdateInfo?> CheckLatestAsync(
        string currentVersion,
        string? skippedVersion,
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync(LatestReleaseApi, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var latest = await ParseLatestAsync(stream, cancellationToken).ConfigureAwait(false);

        if (latest is null ||
            IsSameVersion(latest.TagName, skippedVersion) ||
            CompareVersions(latest.TagName, currentVersion) <= 0)
        {
            return null;
        }

        return latest;
    }

    public static async Task<ReleaseUpdateInfo?> ParseLatestAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        if (!TryGetString(root, "tag_name", out var tagName) ||
            string.IsNullOrWhiteSpace(tagName) ||
            !TryGetString(root, "html_url", out var htmlUrl) ||
            !Uri.TryCreate(htmlUrl, UriKind.Absolute, out var htmlUri))
        {
            return null;
        }

        var title = TryGetString(root, "name", out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : tagName;

        var assets = new List<ReleaseAssetInfo>();
        if (root.TryGetProperty("assets", out var assetsElement) &&
            assetsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetsElement.EnumerateArray())
            {
                if (!TryGetString(asset, "name", out var assetName) ||
                    !TryGetString(asset, "browser_download_url", out var downloadUrl) ||
                    !Uri.TryCreate(downloadUrl, UriKind.Absolute, out var downloadUri))
                {
                    continue;
                }

                assets.Add(new ReleaseAssetInfo(assetName, downloadUri));
            }
        }

        return new ReleaseUpdateInfo(tagName, title, htmlUri, assets);
    }

    public static int CompareVersions(string left, string right)
    {
        var leftVersion = ParseVersion(left);
        var rightVersion = ParseVersion(right);
        return leftVersion.CompareTo(rightVersion);
    }

    private static bool IsSameVersion(string left, string? right)
    {
        return !string.IsNullOrWhiteSpace(right) && CompareVersions(left, right) == 0;
    }

    private static Version ParseVersion(string value)
    {
        var normalized = value.Trim().TrimStart('v', 'V');
        var dashIndex = normalized.IndexOf('-');
        if (dashIndex >= 0)
        {
            normalized = normalized[..dashIndex];
        }

        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (parts.Count < 3)
        {
            parts.Add("0");
        }

        return Version.TryParse(string.Join('.', parts.Take(4)), out var version)
            ? version
            : new Version(0, 0, 0);
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
