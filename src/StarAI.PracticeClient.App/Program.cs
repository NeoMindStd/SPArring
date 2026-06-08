namespace StarAI.PracticeClient.App;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-start", StringComparison.OrdinalIgnoreCase)))
        {
            return SmokeChecks.RunStart(args);
        }

        if (args.Any(arg => string.Equals(arg, "--audit-compatibility", StringComparison.OrdinalIgnoreCase)))
        {
            return CompatibilityAuditCommand.Run();
        }

        if (args.Any(arg => string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase)))
        {
            return SmokeChecks.Run();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
