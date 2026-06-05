using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

internal sealed class PracticeOverlayForm : Form
{
    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _timer;
    private DateTime _startedAtUtc;
    private ActionRateCounter? _counter;

    public PracticeOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.78;
        Size = new Size(230, 38);

        _label = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(166, 255, 126),
            BackColor = Color.Transparent
        };
        Controls.Add(_label);

        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => RefreshText();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExTransparent = 0x00000020;
            const int wsExNoActivate = 0x08000000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= wsExToolWindow | wsExTransparent | wsExNoActivate;
            return parameters;
        }
    }

    public void StartSession(Rectangle screenBounds, DateTime startedAtUtc, ActionRateCounter counter)
    {
        _startedAtUtc = startedAtUtc;
        _counter = counter;
        Location = new Point(screenBounds.Left + 18, screenBounds.Top + 18);
        RefreshText();
        Show();
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RefreshText()
    {
        var elapsed = DateTime.UtcNow - _startedAtUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        var minutes = (int)elapsed.TotalMinutes;
        var seconds = elapsed.Seconds;
        var apm = _counter?.ActionsPerMinute(elapsed) ?? 0;
        _label.Text = $"{minutes:00}:{seconds:00}  APM {apm}";
    }
}
