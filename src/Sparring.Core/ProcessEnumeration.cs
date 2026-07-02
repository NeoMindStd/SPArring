using System.Diagnostics;

namespace Sparring.Core;

public static class ProcessEnumeration
{
    public static HashSet<int> CurrentProcessIdsByName(string processName)
    {
        return Process.GetProcessesByName(processName)
            .Where(IsRunning)
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToHashSet();
    }

    public static bool IsRunning(Process process)
    {
        try
        {
            process.Refresh();
            return !process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch
        {
            // Some legacy 1.16.1 processes can deny metadata access while still running.
            return true;
        }
    }

    public static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return IsRunning(process);
        }
        catch
        {
            return false;
        }
    }
}
