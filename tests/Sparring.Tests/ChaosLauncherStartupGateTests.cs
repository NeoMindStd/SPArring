using Sparring.Core;
using System.Diagnostics;

namespace Sparring.Tests;

public sealed class ChaosLauncherStartupGateTests
{
    [Fact]
    public void EnterSerializesChaosLauncherStartupAcrossThreads()
    {
        Assert.Equal(@"Local\Sparring.ChaosLauncher.Startup", ChaosLauncherStartupGate.MutexName);

        using var first = ChaosLauncherStartupGate.Enter(TimeSpan.FromSeconds(1));
        var stopwatch = Stopwatch.StartNew();

        var enteredWhileHeld = false;
        var worker = new Thread(() =>
        {
            try
            {
                using var second = ChaosLauncherStartupGate.Enter(TimeSpan.FromMilliseconds(100));
                enteredWhileHeld = true;
            }
            catch (TimeoutException)
            {
                enteredWhileHeld = false;
            }
        });
        worker.Start();
        worker.Join();

        Assert.False(enteredWhileHeld);
        Assert.True(stopwatch.ElapsedMilliseconds >= 50);
    }
}
