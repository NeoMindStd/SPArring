using StarAI.PracticeClient.Core;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal sealed class PracticeOverlayForm : Form
{
    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _timer;
    private PracticeSessionClock? _clock;
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
        _clock = new PracticeSessionClock(startedAtUtc);
        _counter = counter;
        Location = new Point(screenBounds.Left + 18, screenBounds.Top + 18);
        RefreshText();
        Show();
        KeepAboveGame();
        _timer.Start();
    }

    public void KeepAboveGame()
    {
        const int hwndTopmost = -1;
        const uint swpNoSize = 0x0001;
        const uint swpNoMove = 0x0002;
        const uint swpNoActivate = 0x0010;
        const uint swpShowWindow = 0x0040;
        if (IsHandleCreated)
        {
            _ = SetWindowPos(
                Handle,
                new IntPtr(hwndTopmost),
                0,
                0,
                0,
                0,
                swpNoMove | swpNoSize | swpNoActivate | swpShowWindow);
        }
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
        if (_clock is not { } clock || _counter is not { } counter)
        {
            _label.Text = "00:00  APM 0";
            return;
        }

        _label.Text = clock.FormatOverlayText(DateTime.UtcNow, counter);
        KeepAboveGame();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);
}
