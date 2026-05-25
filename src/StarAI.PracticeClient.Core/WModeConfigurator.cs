namespace StarAI.PracticeClient.Core;

public static class WModeConfigurator
{
    public static string Apply(string starCraftRoot, bool windowedMode, bool clipCursor)
    {
        var path = Path.Combine(starCraftRoot, "wmode.ini");
        var ini = File.Exists(path)
            ? BwapiIni.Load(path)
            : BwapiIni.Parse("[W-MODE]" + Environment.NewLine);

        ini.Set("W-MODE", "SaveClipCursor", "0");
        ini.Set("W-MODE", "ClipCursor", windowedMode && clipCursor ? "1" : "0");
        ini.Set("W-MODE", "SaveWindowed", "1");
        ini.Set("W-MODE", "Windowed", windowedMode ? "1" : "0");
        ini.Set("W-MODE", "SaveEnableWindowMove", "1");
        ini.Set("W-MODE", "EnableWindowMove", "1");
        ini.Set("W-MODE", "AlwaysOnTop", "0");
        ini.Set("W-MODE", "DisableControls", "0");
        ini.Save(path);
        return path;
    }
}
