namespace StarAI.PracticeClient.Core;

public static class CoachAiLocator
{
    public static string? FindCoachAiDll(string starCraftRoot)
    {
        var aiRoot = Path.Combine(starCraftRoot, "bwapi-data", "AI");
        if (!Directory.Exists(aiRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(aiRoot, "*.dll", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
                Path.GetFileName(path).Contains("CoachAI", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}CoachAI{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }
}
