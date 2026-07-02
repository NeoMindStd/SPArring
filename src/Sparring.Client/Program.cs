using Sparring.Core;

namespace Sparring.Client;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-start", StringComparison.OrdinalIgnoreCase)))
        {
            return SmokeChecks.RunStart(args);
        }

        if (args.Any(arg => string.Equals(arg, "--smoke-bot-match", StringComparison.OrdinalIgnoreCase)))
        {
            return SmokeChecks.RunBotMatch(args);
        }

        if (args.Any(arg => string.Equals(arg, "--audit-compatibility", StringComparison.OrdinalIgnoreCase)))
        {
            return CompatibilityAuditCommand.Run(args);
        }

        if (args.Any(arg => string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase)))
        {
            return SmokeChecks.Run();
        }

        ApplicationConfiguration.Initialize();
        var integrityReport = StartupIntegrityCheck.Run(PracticePaths.Defaults());
        if (integrityReport.ShouldNotify)
        {
            MessageBox.Show(
                StartupIntegrityCheck.FormatUserMessage(integrityReport),
                "Sparring 설치 복구",
                MessageBoxButtons.OK,
                integrityReport.FullyRepaired ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        var form = new MainForm();
        form.Shown += async (_, _) => await form.CheckForUpdatesOnStartupAsync();
        Application.Run(form);
        return 0;
    }
}
