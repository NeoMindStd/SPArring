namespace Sparring.Client;

internal sealed class SparringComboBox : ComboBox
{
    private static readonly Color PanelBackground = Color.Black;
    private static readonly Color PanelBorder = Color.FromArgb(96, 220, 118);
    private static readonly Color PanelBorderFocused = Color.FromArgb(213, 189, 69);
    private static readonly Color TextNormal = Color.FromArgb(166, 255, 126);
    private static readonly Color TextSelected = Color.FromArgb(220, 255, 156);
    private static readonly Color ItemSelected = Color.FromArgb(16, 72, 42);

    public SparringComboBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 24;
        BackColor = PanelBackground;
        ForeColor = TextNormal;
        FlatStyle = FlatStyle.Flat;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        DrawClosedFace(e.Graphics);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        pevent.Graphics.FillRectangle(Brushes.Black, ClientRectangle);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        using var backgroundBrush = new SolidBrush((e.State & DrawItemState.Selected) == DrawItemState.Selected
            ? ItemSelected
            : PanelBackground);
        using var textBrush = new SolidBrush((e.State & DrawItemState.Selected) == DrawItemState.Selected
            ? TextSelected
            : TextNormal);
        using var borderPen = new Pen((e.State & DrawItemState.Selected) == DrawItemState.Selected
            ? PanelBorderFocused
            : Color.FromArgb(43, 118, 64));

        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        e.Graphics.DrawRectangle(
            borderPen,
            e.Bounds.X,
            e.Bounds.Y,
            Math.Max(0, e.Bounds.Width - 1),
            Math.Max(0, e.Bounds.Height - 1));

        if (e.Index >= 0)
        {
            var text = GetItemText(Items[e.Index]);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 2, e.Bounds.Width - 12, e.Bounds.Height - 4),
                textBrush.Color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        e.DrawFocusRectangle();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is WmPaint or WmNcPaint or WmPrintClient)
        {
            DrawChrome();
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);
        Invalidate();
    }

    private void DrawChrome()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0)
        {
            return;
        }

        using var graphics = Graphics.FromHwnd(Handle);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        var arrowWidth = Math.Min(24, Math.Max(18, Height));
        var arrowBounds = new Rectangle(Math.Max(0, Width - arrowWidth), 0, arrowWidth, Height);
        using var backgroundBrush = new SolidBrush(PanelBackground);
        using var borderPen = new Pen(Focused ? PanelBorderFocused : PanelBorder);
        using var separatorPen = new Pen(Color.FromArgb(43, 118, 64));
        using var arrowBrush = new SolidBrush(Focused ? PanelBorderFocused : TextNormal);

        graphics.FillRectangle(backgroundBrush, arrowBounds);
        graphics.DrawLine(separatorPen, arrowBounds.Left, 3, arrowBounds.Left, Math.Max(3, Height - 4));

        var centerX = arrowBounds.Left + arrowBounds.Width / 2;
        var centerY = Height / 2 + 1;
        var arrow = new[]
        {
            new Point(centerX - 4, centerY - 2),
            new Point(centerX + 4, centerY - 2),
            new Point(centerX, centerY + 3)
        };
        graphics.FillPolygon(arrowBrush, arrow);
        graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }

    private void DrawClosedFace(Graphics graphics)
    {
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        using var backgroundBrush = new SolidBrush(PanelBackground);
        using var borderPen = new Pen(Focused ? PanelBorderFocused : PanelBorder);
        using var textBrush = new SolidBrush(TextNormal);
        using var separatorPen = new Pen(Color.FromArgb(43, 118, 64));
        using var arrowBrush = new SolidBrush(Focused ? PanelBorderFocused : TextNormal);

        graphics.FillRectangle(backgroundBrush, ClientRectangle);

        var arrowWidth = Math.Min(24, Math.Max(18, Height));
        var arrowBounds = new Rectangle(Math.Max(0, Width - arrowWidth), 0, arrowWidth, Height);
        var textBounds = new Rectangle(8, 1, Math.Max(0, Width - arrowWidth - 12), Math.Max(0, Height - 2));
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textBounds,
            textBrush.Color,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        graphics.DrawLine(separatorPen, arrowBounds.Left, 3, arrowBounds.Left, Math.Max(3, Height - 4));

        var centerX = arrowBounds.Left + arrowBounds.Width / 2;
        var centerY = Height / 2 + 1;
        var arrow = new[]
        {
            new Point(centerX - 4, centerY - 2),
            new Point(centerX + 4, centerY - 2),
            new Point(centerX, centerY + 3)
        };
        graphics.FillPolygon(arrowBrush, arrow);
        graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }

    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private const int WmPrintClient = 0x0318;
}
