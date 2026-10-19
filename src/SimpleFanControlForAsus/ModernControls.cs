using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class UiPalette
    {
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryDark = Color.FromArgb(30, 64, 175);
        public static readonly Color PrimarySoft = Color.FromArgb(232, 242, 255);
        public static readonly Color Page = Color.FromArgb(248, 250, 252);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(221, 228, 238);
        public static readonly Color Text = Color.FromArgb(24, 35, 52);
        public static readonly Color Muted = Color.FromArgb(71, 85, 105);
        public static readonly Color Disabled = Color.FromArgb(181, 192, 207);
        public static readonly Color Orange = Color.FromArgb(194, 65, 12);
        public static readonly Color Success = Color.FromArgb(24, 145, 102);
        public static readonly Color Danger = Color.FromArgb(201, 62, 74);
    }

    internal static class DrawingHelper
    {
        public static GraphicsPath RoundedRectangle(Rectangle rectangle, int radius)
        {
            var diameter = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Configure(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
        }
    }

    internal sealed class ToggleSwitch : CheckBox
    {
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false;
            Size = new Size(48, 26);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawingHelper.Configure(e.Graphics);
            e.Graphics.Clear(UiPalette.Surface);
            var scale = DeviceDpi / 96f;
            var track = new Rectangle((int)(3 * scale), (Height - (int)(21 * scale)) / 2, Width - (int)(7 * scale), (int)(21 * scale));
            var radius = track.Height / 2;
            using (var path = DrawingHelper.RoundedRectangle(track, radius))
            using (var brush = new SolidBrush(Checked ? UiPalette.Primary : Color.FromArgb(199, 208, 220)))
                e.Graphics.FillPath(brush, path);
            var inset = Math.Max(2, (int)(3 * scale));
            var diameter = track.Height - inset * 2;
            var x = Checked ? track.Right - diameter - inset : track.Left + inset;
            using (var shadow = new SolidBrush(Color.FromArgb(38, 0, 0, 0)))
                e.Graphics.FillEllipse(shadow, x, track.Top + inset + 1, diameter, diameter);
            using (var thumb = new SolidBrush(Color.White))
                e.Graphics.FillEllipse(thumb, x, track.Top + inset, diameter, diameter);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }

    internal sealed class ModernSlider : Control
    {
        private int value = 90;
        private bool dragging;
        public ModernSlider()
        {
            BackColor = UiPalette.Surface;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
            Height = 38;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public event EventHandler ValueChanged;
        public event EventHandler ValueCommitted;
        public int Value
        {
            get
            {
                return value;
            }

            set
            {
                var clamped = Math.Max(0, Math.Min(100, value));
                if (this.value == clamped)
                    return;
                this.value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawingHelper.Configure(e.Graphics);
            var scale = DeviceDpi / 96f;
            var centerY = Height / 2;
            var inset = (int)(10 * scale);
            var left = inset;
            var right = Math.Max(left + 1, Width - inset);
            var thumbX = left + (int)Math.Round((right - left) * Value / 100.0);
            var lineWidth = Math.Max(4, (int)(5 * scale));
            using (var inactive = new Pen(Color.FromArgb(224, 230, 239), lineWidth))
            using (var active = new Pen(Enabled ? UiPalette.Primary : UiPalette.Disabled, lineWidth))
            {
                inactive.StartCap = inactive.EndCap = LineCap.Round;
                active.StartCap = active.EndCap = LineCap.Round;
                e.Graphics.DrawLine(inactive, left, centerY, right, centerY);
                e.Graphics.DrawLine(active, left, centerY, thumbX, centerY);
            }

            var halo = (int)(22 * scale);
            var thumb = (int)(14 * scale);
            using (var haloBrush = new SolidBrush(Color.FromArgb(28, UiPalette.Primary)))
                e.Graphics.FillEllipse(haloBrush, thumbX - halo / 2, centerY - halo / 2, halo, halo);
            using (var thumbBrush = new SolidBrush(Enabled ? UiPalette.Primary : UiPalette.Disabled))
                e.Graphics.FillEllipse(thumbBrush, thumbX - thumb / 2, centerY - thumb / 2, thumb, thumb);
            using (var centerBrush = new SolidBrush(Color.White))
                e.Graphics.FillEllipse(centerBrush, thumbX - 3, centerY - 3, 6, 6);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -1, -1));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left)
                return;
            Focus();
            dragging = true;
            Capture = true;
            SetValueFromMouse(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
                SetValueFromMouse(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging)
                return;
            dragging = false;
            Capture = false;
            SetValueFromMouse(e.X);
            ValueCommitted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled || (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right))
                return;
            Value += e.KeyCode == Keys.Right ? 1 : -1;
            ValueCommitted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || base.IsInputKey(keyData);
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

        private void SetValueFromMouse(int x)
        {
            var inset = Math.Max(9, (int)(10 * DeviceDpi / 96f));
            var width = Math.Max(1, Width - inset * 2);
            Value = (int)Math.Round(Math.Max(0, Math.Min(1, (x - inset) / (double)width)) * 100);
        }
    }

    internal sealed class ModernCard : Panel
    {
        public ModernCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = new Padding(20);
            Margin = Padding.Empty;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(UiPalette.Page);
            DrawingHelper.Configure(e.Graphics);
            var cardRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var cardPath = DrawingHelper.RoundedRectangle(cardRect, Math.Max(6, (int)(8 * DeviceDpi / 96f))))
            using (var cardBrush = new SolidBrush(UiPalette.Surface))
            using (var borderPen = new Pen(UiPalette.Border))
            {
                e.Graphics.FillPath(cardBrush, cardPath);
                e.Graphics.DrawPath(borderPen, cardPath);
            }
        }
    }

    internal sealed class LanguageButton : Control
    {
        private UiLanguage language;
        public LanguageButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Size = new Size(76, 38);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public UiLanguage Language
        {
            get
            {
                return language;
            }

            set
            {
                language = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawingHelper.Configure(e.Graphics);
            e.Graphics.Clear(UiPalette.Surface);
            var rectangle = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DrawingHelper.RoundedRectangle(rectangle, 9))
            using (var fill = new SolidBrush(UiPalette.Surface))
            using (var border = new Pen(Focused ? UiPalette.Primary : UiPalette.Border, Focused ? 2 : 1))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }

            var scale = DeviceDpi / 96f;
            var flag = new Rectangle((int)(10 * scale), (Height - (int)(18 * scale)) / 2, (int)(24 * scale), (int)(18 * scale));
            if (Language == UiLanguage.Vietnamese)
                DrawVietnamFlag(e.Graphics, flag);
            else
                DrawUsFlag(e.Graphics, flag);
            var code = Language == UiLanguage.Vietnamese ? "VI" : "EN";
            TextRenderer.DrawText(e.Graphics, code, Font, new Rectangle((int)(40 * scale), 0, Width - (int)(40 * scale), Height), UiPalette.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
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

        private static void DrawVietnamFlag(Graphics graphics, Rectangle rectangle)
        {
            using (var red = new SolidBrush(Color.FromArgb(218, 37, 29)))
                graphics.FillRectangle(red, rectangle);
            using (var star = CreateStar(new PointF(rectangle.Left + rectangle.Width / 2f, rectangle.Top + rectangle.Height / 2f), rectangle.Height * .30f, rectangle.Height * .12f))
            using (var yellow = new SolidBrush(Color.FromArgb(255, 221, 0)))
                graphics.FillPath(yellow, star);
        }

        private static void DrawUsFlag(Graphics graphics, Rectangle rectangle)
        {
            using (var white = new SolidBrush(Color.White))
                graphics.FillRectangle(white, rectangle);
            using (var red = new SolidBrush(Color.FromArgb(191, 10, 48)))
            {
                for (var stripe = 0; stripe < 7; stripe++)
                {
                    var y = rectangle.Top + stripe * rectangle.Height / 7;
                    graphics.FillRectangle(red, rectangle.Left, y, rectangle.Width, Math.Max(1, rectangle.Height / 14));
                }
            }

            using (var blue = new SolidBrush(Color.FromArgb(0, 40, 104)))
                graphics.FillRectangle(blue, rectangle.Left, rectangle.Top, rectangle.Width / 2, rectangle.Height / 2 + 1);
            using (var dot = new SolidBrush(Color.White))
            {
                for (var row = 0; row < 3; row++)
                    for (var column = 0; column < 4; column++)
                        graphics.FillEllipse(dot, rectangle.Left + 2 + column * 3, rectangle.Top + 2 + row * 3, 1.4f, 1.4f);
            }
        }

        private static GraphicsPath CreateStar(PointF center, float outerRadius, float innerRadius)
        {
            var points = new PointF[10];
            for (var index = 0; index < points.Length; index++)
            {
                var angle = -Math.PI / 2 + index * Math.PI / 5;
                var radius = index % 2 == 0 ? outerRadius : innerRadius;
                points[index] = new PointF(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius);
            }

            var path = new GraphicsPath();
            path.AddPolygon(points);
            return path;
        }
    }

    internal enum FanControlMode
    {
        Off,
        Manual,
        Curve
    }

    internal sealed class ModeSelector : Control
    {
        private FanControlMode mode;
        public string OffText { get; set; } = "Off";
        public string ManualText { get; set; } = "Manual";
        public string CurveText { get; set; } = "Curve";

        public bool CurveSelected
        {
            get
            {
                return mode == FanControlMode.Curve;
            }
        }

        public event EventHandler SelectionChanged;
        public FanControlMode SelectedMode
        {
            get
            {
                return mode;
            }

            set
            {
                if (mode == value)
                    return;
                mode = value;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ModeSelector()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Height = 44;
            TabStop = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawingHelper.Configure(e.Graphics);
            e.Graphics.Clear(UiPalette.Surface);
            using (var path = DrawingHelper.RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (var fill = new SolidBrush(Color.FromArgb(241, 245, 249)))
                e.Graphics.FillPath(fill, path);
            var labels = new[]
            {
                OffText,
                ManualText,
                CurveText
            };
            for (int i = 0; i < 3; i++)
            {
                var bounds = new Rectangle(i * Width / 3 + 3, 3, Width / 3 - 6, Height - 6);
                bool selected = (int)mode == i;
                if (selected)
                    using (var path = DrawingHelper.RoundedRectangle(bounds, 6))
                    using (var fill = new SolidBrush(UiPalette.Surface))
                    using (var border = new Pen(UiPalette.Border))
                    {
                        e.Graphics.FillPath(fill, path);
                        e.Graphics.DrawPath(border, path);
                    }

                TextRenderer.DrawText(e.Graphics, labels[i], Font, bounds, selected ? UiPalette.PrimaryDark : UiPalette.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }

            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -2, -2));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
                return;
            Focus();
            SelectedMode = (FanControlMode)Math.Max(0, Math.Min(2, e.X * 3 / Math.Max(1, Width)));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                SelectedMode = (FanControlMode)Math.Max(0, Math.Min(2, (int)mode + (e.KeyCode == Keys.Right ? 1 : -1)));
                e.Handled = true;
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
    }
}
