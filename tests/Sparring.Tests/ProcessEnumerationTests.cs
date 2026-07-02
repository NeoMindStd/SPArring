using System.Diagnostics;
using Sparring.Core;

namespace Sparring.Tests;

public sealed class ProcessEnumerationTests
{
    [Fact]
    public void IsRunningReturnsTrueForCurrentProcess()
    {
        using var process = Process.GetCurrentProcess();

        Assert.True(ProcessEnumeration.IsRunning(process));
    }

    [Fact]
    public void IsRunningReturnsFalseForExitedProcess()
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });
        Assert.NotNull(process);
        process.WaitForExit();

        Assert.False(ProcessEnumeration.IsRunning(process));
    }

    [Fact]
    public void IsProcessRunningReturnsFalseForExitedProcessId()
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });
        Assert.NotNull(process);
        var processId = process.Id;
        process.WaitForExit();

        Assert.False(ProcessEnumeration.IsProcessRunning(processId));
    }
}
