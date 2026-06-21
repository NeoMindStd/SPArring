namespace StarAI.PracticeClient.Setup;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args is ["--ui-smoke", var screenshotPath])
        {
            RenderUiSmoke(screenshotPath);
            return 0;
        }

        Application.Run(new SetupForm());
        return 0;
    }

    private static void RenderUiSmoke(string screenshotPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(screenshotPath))!);
        using var form = new SetupForm();
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-32000, -32000);
        form.Show();
        form.Refresh();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        bitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
        form.Close();
    }
}
