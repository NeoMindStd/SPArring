using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

#pragma warning disable CA1416

namespace StarAI.PracticeClient.Core;

public enum RegistryHiveKind
{
    CurrentUser,
    LocalMachine
}

public sealed record RegistryValueState(bool Exists, object? Value, RegistryValueKind Kind);

public interface IRegistryAccess
{
    RegistryValueState ReadValue(RegistryHiveKind hive, string subKey, string name);

    void WriteValue(RegistryHiveKind hive, string subKey, string name, object value, RegistryValueKind kind);

    void DeleteValue(RegistryHiveKind hive, string subKey, string name);
}

public sealed record ChaosLauncherRequest(
    string RuntimeRoot,
    bool RunStarCraftOnStartup,
    bool EnableWMode,
    bool EnableBwapi,
    bool EnableApmAlert);

public sealed class RegistryRestorePoint
{
    private readonly IRegistryAccess _registry;
    private readonly IReadOnlyList<RegistryValueMutation> _values;
    private bool _restored;

    internal RegistryRestorePoint(IRegistryAccess registry, IReadOnlyList<RegistryValueMutation> values)
    {
        _registry = registry;
        _values = values;
    }

    public void Restore()
    {
        if (_restored)
        {
            return;
        }

        foreach (var value in _values)
        {
            if (value.Original.Exists)
            {
                _registry.WriteValue(value.Hive, value.SubKey, value.Name, value.Original.Value!, value.Original.Kind);
            }
            else
            {
                _registry.DeleteValue(value.Hive, value.SubKey, value.Name);
            }
        }

        _restored = true;
    }
}

public sealed class ChaosLauncherConfigurator
{
    public const string LauncherKey = @"Software\Chaoslauncher\Launcher";
    public const string EnabledKey = @"Software\Chaoslauncher\PluginsEnabled";
    public const string RunIncompatibleKey = @"Software\Chaoslauncher\PluginsRunIncompatible";
    public const string StarCraftInstallKey = @"SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft";
    public const string BwapiPlugin = "BWAPI 4.4.0 Injector [RELEASE]";
    public const string BwapiDebugPlugin = "BWAPI 4.4.0 Injector [DEBUG]";
    public const string WModePlugin = "W-MODE 1.02";
    public const string ApmAlertPlugin = "APMAlert (1.16.1)";

    private readonly IRegistryAccess _registry;

    public ChaosLauncherConfigurator()
        : this(new WindowsRegistryAccess())
    {
    }

    public ChaosLauncherConfigurator(IRegistryAccess registry)
    {
        _registry = registry;
    }

    public RegistryRestorePoint ApplyWithRestorePoint(ChaosLauncherRequest request)
    {
        var writes = BuildWrites(request);
        var restoreValues = writes
            .Select(write => new RegistryValueMutation(
                write.Hive,
                write.SubKey,
                write.Name,
                _registry.ReadValue(write.Hive, write.SubKey, write.Name)))
            .ToList();

        foreach (var write in writes)
        {
            _registry.WriteValue(write.Hive, write.SubKey, write.Name, write.Value, write.Kind);
        }

        return new RegistryRestorePoint(_registry, restoreValues);
    }

    public IReadOnlyList<RegistryValueWrite> BuildWrites(ChaosLauncherRequest request)
    {
        var root = Path.GetFullPath(request.RuntimeRoot);
        var starCraftExe = Path.Combine(root, "StarCraft.exe");

        return
        [
            Dword(RegistryHiveKind.CurrentUser, LauncherKey, "AutoUpdate", 0),
            String(RegistryHiveKind.CurrentUser, LauncherKey, "GameVersion", "Starcraft 1.16.1"),
            Dword(RegistryHiveKind.CurrentUser, LauncherKey, "MinimizeOnRun", 0),
            Dword(RegistryHiveKind.CurrentUser, LauncherKey, "StartMinimized", 0),
            Dword(RegistryHiveKind.CurrentUser, LauncherKey, "WarnNoAdmin", 0),
            Dword(RegistryHiveKind.CurrentUser, LauncherKey, "RunScOnStartup", request.RunStarCraftOnStartup ? 1 : 0),
            Dword(RegistryHiveKind.CurrentUser, EnabledKey, WModePlugin, request.EnableWMode ? 1 : 0),
            Dword(RegistryHiveKind.CurrentUser, EnabledKey, BwapiPlugin, request.EnableBwapi ? 1 : 0),
            Dword(RegistryHiveKind.CurrentUser, EnabledKey, BwapiDebugPlugin, 0),
            Dword(RegistryHiveKind.CurrentUser, EnabledKey, ApmAlertPlugin, request.EnableApmAlert ? 1 : 0),
            Dword(RegistryHiveKind.CurrentUser, RunIncompatibleKey, WModePlugin, 0),
            Dword(RegistryHiveKind.CurrentUser, RunIncompatibleKey, BwapiPlugin, 0),
            Dword(RegistryHiveKind.CurrentUser, RunIncompatibleKey, BwapiDebugPlugin, 0),
            Dword(RegistryHiveKind.CurrentUser, RunIncompatibleKey, ApmAlertPlugin, 0),
            String(RegistryHiveKind.LocalMachine, StarCraftInstallKey, "InstallPath", root),
            String(RegistryHiveKind.LocalMachine, StarCraftInstallKey, "Program", starCraftExe)
        ];
    }

    private static RegistryValueWrite Dword(RegistryHiveKind hive, string subKey, string name, int value)
    {
        return new RegistryValueWrite(hive, subKey, name, value, RegistryValueKind.DWord);
    }

    private static RegistryValueWrite String(RegistryHiveKind hive, string subKey, string name, string value)
    {
        return new RegistryValueWrite(hive, subKey, name, value, RegistryValueKind.String);
    }
}

public sealed class ChaosLauncherClient
{
    private readonly ChaosLauncherConfigurator _configurator;

    public ChaosLauncherClient()
        : this(new ChaosLauncherConfigurator())
    {
    }

    public ChaosLauncherClient(ChaosLauncherConfigurator configurator)
    {
        _configurator = configurator;
    }

    [SupportedOSPlatform("windows")]
    public ChaosLauncherRun Start(ChaosLauncherRequest request)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("ChaosLauncher can only be started on Windows.");
        }

        var launcher = Path.Combine(request.RuntimeRoot, "Chaoslauncher - MultiInstance.exe");
        if (!File.Exists(launcher))
        {
            throw new FileNotFoundException("ChaosLauncher MultiInstance executable not found.", launcher);
        }

        var restorePoint = _configurator.ApplyWithRestorePoint(request);
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = launcher,
                WorkingDirectory = request.RuntimeRoot,
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("Failed to start ChaosLauncher.");

            return new ChaosLauncherRun(process, restorePoint);
        }
        catch
        {
            restorePoint.Restore();
            throw;
        }
    }
}

public sealed record ChaosLauncherRun(Process Process, RegistryRestorePoint RestorePoint);

public static class ChaosLauncherLog
{
    public const string FileName = "Chaoslauncher - MultiInstance.log";

    public static string PathForRuntime(string runtimeRoot)
    {
        return Path.Combine(runtimeRoot, FileName);
    }

    public static int CountCompletedStarts(string runtimeRoot)
    {
        var path = PathForRuntime(runtimeRoot);
        if (!File.Exists(path))
        {
            return 0;
        }

        try
        {
            return File.ReadLines(path)
                .Count(line => line.Contains("Starting Starcraft completed", StringComparison.OrdinalIgnoreCase));
        }
        catch (IOException)
        {
            return 0;
        }
    }

    public static void WaitForCompletedStart(string runtimeRoot, int previousCount, TimeSpan timeout, DateTime requestedAtUtc)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var currentCount = CountCompletedStarts(runtimeRoot);
            if (currentCount > previousCount || LogWasRewrittenForThisRequest(runtimeRoot, currentCount, requestedAtUtc))
            {
                return;
            }

            Thread.Sleep(250);
        }

        throw new InvalidOperationException($"StarCraft launch did not complete within {timeout.TotalSeconds:0} seconds: {runtimeRoot}");
    }

    private static bool LogWasRewrittenForThisRequest(string runtimeRoot, int currentCount, DateTime requestedAtUtc)
    {
        if (currentCount <= 0)
        {
            return false;
        }

        try
        {
            return File.GetLastWriteTimeUtc(PathForRuntime(runtimeRoot)) >= requestedAtUtc.AddSeconds(-1);
        }
        catch (IOException)
        {
            return false;
        }
    }
}

public sealed class WindowsRegistryAccess : IRegistryAccess
{
    [SupportedOSPlatform("windows")]
    public RegistryValueState ReadValue(RegistryHiveKind hive, string subKey, string name)
    {
        using var key = OpenBaseKey(hive).OpenSubKey(subKey, writable: false);
        if (key is null)
        {
            return new RegistryValueState(false, null, RegistryValueKind.Unknown);
        }

        var value = key.GetValue(name, null);
        if (value is null && !key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return new RegistryValueState(false, null, RegistryValueKind.Unknown);
        }

        return new RegistryValueState(true, value, key.GetValueKind(name));
    }

    [SupportedOSPlatform("windows")]
    public void WriteValue(RegistryHiveKind hive, string subKey, string name, object value, RegistryValueKind kind)
    {
        using var key = OpenBaseKey(hive).CreateSubKey(subKey, writable: true)
            ?? throw new InvalidOperationException($"Unable to open registry key: {hive}\\{subKey}");
        key.SetValue(name, value, kind);
    }

    [SupportedOSPlatform("windows")]
    public void DeleteValue(RegistryHiveKind hive, string subKey, string name)
    {
        using var key = OpenBaseKey(hive).OpenSubKey(subKey, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }

    [SupportedOSPlatform("windows")]
    private static RegistryKey OpenBaseKey(RegistryHiveKind hive)
    {
        return hive switch
        {
            RegistryHiveKind.CurrentUser => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default),
            RegistryHiveKind.LocalMachine => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32),
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null)
        };
    }
}

public sealed record RegistryValueWrite(
    RegistryHiveKind Hive,
    string SubKey,
    string Name,
    object Value,
    RegistryValueKind Kind);

internal sealed record RegistryValueMutation(
    RegistryHiveKind Hive,
    string SubKey,
    string Name,
    RegistryValueState Original);
