namespace StarAI.PracticeClient.Core;

public static class CrashLogInspector
{
    public static bool HasRecentApmAlertCrash(string starCraftRoot, TimeSpan maxAge)
    {
        var errorDirectory = Path.Combine(starCraftRoot, "Errors");
        if (!Directory.Exists(errorDirectory))
        {
            return false;
        }

        var cutoff = DateTime.Now - maxAge;
        foreach (var file in Directory.EnumerateFiles(errorDirectory, "*.ERR"))
        {
            var info = new FileInfo(file);
            if (info.LastWriteTime < cutoff)
            {
                continue;
            }

            try
            {
                var text = File.ReadAllText(file);
                if (text.Contains(@"Plugins\APMAlert.bwl", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("APMAlert.bwl", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                continue;
            }
        }

        return false;
    }
}
