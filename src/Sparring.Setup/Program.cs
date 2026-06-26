namespace Sparring.Setup;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length >= 2 && args[0] == "--ui-smoke")
        {
            return RenderUiSmoke(args);
        }

        Application.Run(new SetupForm());
        return 0;
    }

    private static int RenderUiSmoke(string[] args)
    {
        var screenshotPath = args[1];
        var fontSize = 9F;
        var validate = false;
        for (var index = 2; index < args.Length; index++)
        {
            if (args[index] == "--font-size" && index + 1 < args.Length)
            {
                fontSize = float.Parse(args[++index], System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (args[index] == "--validate")
            {
                validate = true;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(screenshotPath))!);
        using var form = new SetupForm(fontSize);
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-32000, -32000);
        form.Show();
        form.Refresh();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        bitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
        if (validate)
        {
            var issues = FindLayoutIssues(form).ToList();
            if (issues.Count > 0)
            {
                Console.Error.WriteLine("Setup UI layout smoke failed:");
                foreach (var issue in issues)
                {
                    Console.Error.WriteLine("- " + issue);
                }

                form.Close();
                return 1;
            }
        }

        form.Close();
        return 0;
    }

    private static IEnumerable<string> FindLayoutIssues(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (!control.Visible)
            {
                continue;
            }

            if (control.Parent is { } parent &&
                parent is not ScrollableControl { AutoScroll: true })
            {
                var parentBounds = parent.ClientRectangle;
                if (control.Right > parentBounds.Right + 2 || control.Bottom > parentBounds.Bottom + 2)
                {
                    yield return $"{Describe(control)} exceeds parent bounds {parentBounds}.";
                }
            }

            if (control is Button button)
            {
                var preferred = TextRenderer.MeasureText(button.Text, button.Font);
                if (preferred.Width + 20 > button.ClientSize.Width ||
                    preferred.Height + 8 > button.ClientSize.Height)
                {
                    yield return $"{Describe(button)} text does not fit. Text='{button.Text}', preferred={preferred}, size={button.ClientSize}.";
                }
            }

            foreach (var issue in FindLayoutIssues(control))
            {
                yield return issue;
            }
        }
    }

    private static string Describe(Control control)
    {
        var text = string.IsNullOrWhiteSpace(control.Text) ? control.Name : control.Text;
        return $"{control.GetType().Name}({text})";
    }
}
