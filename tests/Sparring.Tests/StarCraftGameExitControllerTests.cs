using System.Diagnostics;
using Sparring.Client;

namespace Sparring.Tests;

public sealed class StarCraftGameExitControllerTests
{
    [Fact]
    public void LeaveGameSequenceUsesStarCraftQuitMenuHotkeys()
    {
        Assert.Equal(
            [StarCraftExitKey.F10, StarCraftExitKey.Q, StarCraftExitKey.Q],
            StarCraftGameExitController.LeaveGameSequence);
    }

    [Fact]
    public void ShouldSendLeaveSequenceAllowsCoveredWindowStates()
    {
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.InGame));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.PreGameWait));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.BlockedDialog));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.Unknown));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.MenuLike));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.GameRoom));
    }

    [Fact]
    public void IsLeaveCompleteStateOnlyAcceptsStableNonGameScreens()
    {
        Assert.True(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.GameRoom));
        Assert.True(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.MenuLike));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.InGame));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.PreGameWait));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.BlockedDialog));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.Unknown));
    }

    [Fact]
    public void GracefulAiShutdownRequiresLeaveSequenceAndCleanExit()
    {
        var normalLeave = new StarCraftAiShutdownResult(
            ProcessWasRunning: true,
            LeaveSequenceSent: true,
            Exited: true,
            LeaveConfirmed: true,
            ForcedKillUsed: false);
        var cleanCloseAfterLeaveAttempt = normalLeave with { LeaveConfirmed = false };
        var delayedCleanCloseAfterLeaveAttempt = normalLeave with
        {
            Exited = false,
            LeaveConfirmed = false
        };
        var forcedDisconnect = normalLeave with
        {
            LeaveSequenceSent = false,
            LeaveConfirmed = true,
            ForcedKillUsed = true
        };

        Assert.True(StarCraftGameExitController.IsGracefulAiShutdown(
            normalLeave,
            processGoneAfterCleanup: true,
            playerStateAfterAiShutdown: StarCraftScreenState.InGame,
            runtimeErrorsClean: true));
        Assert.True(StarCraftGameExitController.IsGracefulAiShutdown(
            cleanCloseAfterLeaveAttempt,
            processGoneAfterCleanup: true,
            playerStateAfterAiShutdown: StarCraftScreenState.MenuLike,
            runtimeErrorsClean: true));
        Assert.True(StarCraftGameExitController.IsGracefulAiShutdown(
            delayedCleanCloseAfterLeaveAttempt,
            processGoneAfterCleanup: true,
            playerStateAfterAiShutdown: StarCraftScreenState.Unknown,
            runtimeErrorsClean: true));
        Assert.False(StarCraftGameExitController.IsGracefulAiShutdown(
            forcedDisconnect,
            processGoneAfterCleanup: true,
            playerStateAfterAiShutdown: StarCraftScreenState.InGame,
            runtimeErrorsClean: true));
    }

    [Fact]
    public void DisconnectProcessWithoutBwapiLeaveTreatsAlreadyExitedProcessAsClean()
    {
        var result = StarCraftGameExitController.DisconnectProcessWithoutBwapiLeave(
            int.MaxValue,
            TimeSpan.FromMilliseconds(1));

        Assert.False(result.ProcessWasRunning);
        Assert.False(result.LeaveSequenceSent);
        Assert.True(result.Exited);
        Assert.True(result.LeaveConfirmed);
        Assert.False(result.ForcedKillUsed);
    }

    [Fact]
    public void IsExpectedBwapiShutdownCrashTextOnlyMatchesKnownDestroyGameShutdown()
    {
        var shutdownCrash = """
            EXCEPTION: 0xE06D7363    UNKNOWN
            STACK:
              BWAPI.dll         0x1003597B
              StarCraft.exe     0x004EE909    DestroyGame
              StarCraft.exe     0x004E0801    preLoadGame
            """;
        var unrelatedCrash = """
            EXCEPTION: 0xE06D7363    UNKNOWN
            STACK:
              BWAPI.dll         0x1003597B
              StarCraft.exe     0x004AAAAA    duringMatch
            """;

        Assert.True(StarCraftGameExitController.IsExpectedBwapiShutdownCrashText(shutdownCrash));
        Assert.False(StarCraftGameExitController.IsExpectedBwapiShutdownCrashText(unrelatedCrash));
    }

    [Fact]
    public void DisconnectProcessWithoutBwapiLeaveRemovesLateExpectedTxtAndErrShutdownCrashPair()
    {
        var errorDirectory = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(errorDirectory);
        using var process = StartLongRunningProcess();

        var writer = new Thread(() =>
        {
            Thread.Sleep(250);
            File.WriteAllText(Path.Combine(errorDirectory, "2026 Jun 24.txt"), KnownShutdownCrashText);
            File.WriteAllText(Path.Combine(errorDirectory, "dwj19160101.ERR"), KnownShutdownCrashErrText);
        });
        writer.Start();

        var result = StarCraftGameExitController.DisconnectProcessWithoutBwapiLeave(
            process.Id,
            TimeSpan.FromMilliseconds(250),
            errorDirectory);

        writer.Join(TimeSpan.FromSeconds(5));

        Assert.True(result.Exited);
        Assert.Empty(Directory.EnumerateFiles(errorDirectory));
    }

    [Fact]
    public void DisconnectProcessWithoutBwapiLeaveRetriesLockedExpectedShutdownCrashPair()
    {
        var errorDirectory = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(errorDirectory);
        using var process = StartLongRunningProcess();
        var textPath = Path.Combine(errorDirectory, "2026 Jun 24.txt");
        var errPath = Path.Combine(errorDirectory, "dwj19160101.ERR");

        var writer = new Thread(() =>
        {
            Thread.Sleep(250);
            using (var text = new FileStream(textPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var err = new FileStream(errPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                WriteText(text, KnownShutdownCrashText);
                WriteText(err, KnownShutdownCrashErrText);
                Thread.Sleep(3500);
            }
        });
        writer.Start();

        var result = StarCraftGameExitController.DisconnectProcessWithoutBwapiLeave(
            process.Id,
            TimeSpan.FromMilliseconds(250),
            errorDirectory);

        writer.Join(TimeSpan.FromSeconds(10));

        Assert.True(result.Exited);
        Assert.Empty(Directory.EnumerateFiles(errorDirectory));
    }

    [Fact]
    public void DisconnectProcessWithoutBwapiLeaveKeepsRetryingLongLockedExpectedShutdownCrashPair()
    {
        var errorDirectory = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(errorDirectory);
        using var process = StartLongRunningProcess();
        var textPath = Path.Combine(errorDirectory, "2026 Jun 24.txt");
        var errPath = Path.Combine(errorDirectory, "dwj19160101.ERR");

        var writer = new Thread(() =>
        {
            Thread.Sleep(250);
            using (var text = new FileStream(textPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var err = new FileStream(errPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                WriteText(text, KnownShutdownCrashText);
                WriteText(err, KnownShutdownCrashErrText);
                Thread.Sleep(9000);
            }
        });
        writer.Start();

        var result = StarCraftGameExitController.DisconnectProcessWithoutBwapiLeave(
            process.Id,
            TimeSpan.FromMilliseconds(250),
            errorDirectory);

        writer.Join(TimeSpan.FromSeconds(15));

        Assert.True(result.Exited);
        Assert.Empty(Directory.EnumerateFiles(errorDirectory));
    }

    [Fact]
    public void RemoveExpectedShutdownCrashesCanCleanExternalRuntimeStopPair()
    {
        var errorDirectory = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(errorDirectory);
        File.WriteAllText(Path.Combine(errorDirectory, "2026 Jun 24.txt"), KnownShutdownCrashText);
        File.WriteAllText(Path.Combine(errorDirectory, "dwj19160101.ERR"), KnownShutdownCrashErrText);

        StarCraftGameExitController.RemoveExpectedShutdownCrashes(errorDirectory, TimeSpan.Zero);

        Assert.Empty(Directory.EnumerateFiles(errorDirectory));
    }

    [Fact]
    public void RemoveExpectedShutdownCrashesWaitsForDelayedFirstExpectedShutdownCrashPair()
    {
        var errorDirectory = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(errorDirectory);

        var writer = new Thread(() =>
        {
            Thread.Sleep(1500);
            File.WriteAllText(Path.Combine(errorDirectory, "2026 Jun 24.txt"), KnownShutdownCrashText);
            File.WriteAllText(Path.Combine(errorDirectory, "dwj19160101.ERR"), KnownShutdownCrashErrText);
        });
        writer.Start();

        StarCraftGameExitController.RemoveExpectedShutdownCrashes(
            errorDirectory,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(3));

        writer.Join(TimeSpan.FromSeconds(5));

        Assert.Empty(Directory.EnumerateFiles(errorDirectory));
    }

    private static void WriteText(FileStream stream, string text)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(text);
        writer.Flush();
        stream.Flush(true);
        stream.Position = 0;
    }

    private static Process StartLongRunningProcess()
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c timeout /t 30 /nobreak > nul",
            CreateNoWindow = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start test process.");
    }

    private const string KnownShutdownCrashText = """
        EXCEPTION: 0xE06D7363    UNKNOWN
        STACK:
          KERNELBASE.dll    0x767D59A4    RaiseException
          VCRUNTIME140.dll  0x6EAB4977    _CxxThrowException
          BWAPI.dll         0x1003597B      ----
          BWAPI.dll         0x1004B0F5      ----
          StarCraft.exe     0x004EE909    DestroyGame
          StarCraft.exe     0x004E0801    preLoadGame
        """;

    private const string KnownShutdownCrashErrText = """
        Exception code: E06D7363 *unknown*
        Fault address: 767D59A4 01:001649A4 C:\WINDOWS\System32\KERNELBASE.dll

        Call stack:
        Address  Frame    Logical addr  Module
        767D59A4 001AFAD8 0001:001649A4 C:\WINDOWS\System32\KERNELBASE.dll
        6EAB4977 001AFB0C 0001:00003977 C:\WINDOWS\SYSTEM32\VCRUNTIME140.dll
        1003597B 001AFCFC 0001:0003497B C:\sparring\SC116AI_ai\bwapi-data\BWAPI.dll
        1004B0F5 001AFD0C 0001:0004A0F5 C:\sparring\SC116AI_ai\bwapi-data\BWAPI.dll
        004EE909 001AFE20 0001:000ED909 C:\sparring\SC116AI_ai\StarCraft.exe
        004E0801 001AFE40 0001:000DF801 C:\sparring\SC116AI_ai\StarCraft.exe
        """;
}
