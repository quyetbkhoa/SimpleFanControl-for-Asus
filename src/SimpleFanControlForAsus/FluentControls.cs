using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal enum UiGlyph
    {
        None,
        Fan,
        Dashboard,
        Settings,
        Temperature,
        Power,
        Reset
    }

    internal static class VectorIcons
    {
        public static void Draw(Graphics graphics, UiGlyph icon, Rectangle bounds, Color color)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(bounds.Left, bounds.Top);
            graphics.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
            using (var pen = new Pen(color, 1.7f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            }

            )
            {
                switch (icon)
                {
                    case UiGlyph.Fan:
                        graphics.DrawEllipse(pen, 9, 9, 6, 6);
                        for (int i = 0; i < 3; i++)
                        {
                            graphics.DrawBezier(pen, 12, 9, 5, -3, 23, 1, 16, 10);
                            graphics.TranslateTransform(12, 12);
                            graphics.RotateTransform(120);
                            graphics.TranslateTransform(-12, -12);
                        }

                        break;
                    case UiGlyph.Dashboard:
                        graphics.DrawRectangle(pen, 3, 3, 7, 8);
                        graphics.DrawRectangle(pen, 14, 3, 7, 5);
                        graphics.DrawRectangle(pen, 3, 15, 7, 6);
                        graphics.DrawRectangle(pen, 14, 12, 7, 9);
                        break;
                    case UiGlyph.Settings:
                        graphics.DrawEllipse(pen, 4, 4, 16, 16);
                        graphics.DrawEllipse(pen, 9, 9, 6, 6);
                        for (int i = 0; i < 8; i++)
                        {
                            graphics.DrawLine(pen, 12, 1, 12, 4);
                            graphics.TranslateTransform(12, 12);
                            graphics.RotateTransform(45);
                            graphics.TranslateTransform(-12, -12);
                        }

                        break;
                    case UiGlyph.Temperature:
                        graphics.DrawArc(pen, 9, 2, 6, 6, 180, 180);
                        graphics.DrawLine(pen, 9, 5, 9, 14);
                        graphics.DrawLine(pen, 15, 5, 15, 14);
                        graphics.DrawArc(pen, 7, 12, 10, 10, -40, 260);
                        graphics.DrawLine(pen, 12, 9, 12, 18);
                        graphics.DrawLine(pen, 18, 6, 21, 6);
                        graphics.DrawLine(pen, 18, 10, 20, 10);
                        break;
                    case UiGlyph.Power:
                        graphics.DrawArc(pen, 4, 4, 16, 16, -50, 280);
                        graphics.DrawLine(pen, 12, 2, 12, 11);
                        break;
                    case UiGlyph.Reset:
                        graphics.DrawArc(pen, 4, 4, 16, 16, -90, 285);
                        graphics.DrawLines(pen, new[] { new Point(4, 2), new Point(4, 8), new Point(10, 8) });
                        break;
                }
            }

            graphics.Restore(state);
        }
    }

    internal sealed class IconView : Control
    {
        public UiGlyph Glyph { get; set; }

        public IconView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            TabStop = false;
            AccessibleRole = AccessibleRole.None;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawingHelper.Configure(e.Graphics);
            e.Graphics.Clear(BackColor);
            int side = Math.Max(12, Math.Min(Width, Height) - 12);
            VectorIcons.Draw(e.Graphics, Glyph, new Rectangle((Width - side) / 2, (Height - side) / 2, side, side), ForeColor);
        }
    }

    internal sealed class FluentButton : Button
    {
        private bool hover;
        private bool pressed;
        public bool Primary { get; set; }
        public bool IsSelected { get; set; }
        public bool Navigation { get; set; }
        public UiGlyph Glyph { get; set; }

        public FluentButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Height = 36;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = UiPalette.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawingHelper.Configure(e.Graphics);
            e.Graphics.Clear(Parent == null || Parent.BackColor.A == 0 ? UiPalette.Surface : Parent.BackColor);
            var bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            var fill = Primary ? UiPalette.Primary : IsSelected ? UiPalette.PrimarySoft : hover ? Color.FromArgb(239, 243, 249) : UiPalette.Surface;
            if (pressed)
                fill = Primary ? UiPalette.PrimaryDark : Color.FromArgb(222, 232, 247);
            var foreground = !Enabled ? UiPalette.Muted : Primary ? Color.White : IsSelected ? UiPalette.PrimaryDark : UiPalette.Text;
            using (var path = DrawingHelper.RoundedRectangle(bounds, Math.Max(4, (int)(6 * DeviceDpi / 96f))))
            using (var brush = new SolidBrush(fill))
            using (var border = new Pen(Focused ? UiPalette.Primary : Navigation || Primary ? fill : UiPalette.Border, Focused ? 2 : 1))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(border, path);
            }

            int inset = (int)(12 * DeviceDpi / 96f);
            var textBounds = new Rectangle(inset, 0, Width - inset * 2, Height);
            if (Glyph != UiGlyph.None)
            {
                int size = (int)(18 * DeviceDpi / 96f);
                VectorIcons.Draw(e.Graphics, Glyph, new Rectangle(inset, (Height - size) / 2, size, size), foreground);
                textBounds.X += size + inset / 2;
                textBounds.Width -= size + inset / 2;
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | (Navigation || Glyph != UiGlyph.None ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter));
            if (Navigation && IsSelected)
                using (var pen = new Pen(UiPalette.Primary, 2))
                    e.Graphics.DrawLine(pen, inset, Height - 3, Width - inset, Height - 3);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            pressed = e.Button == MouseButtons.Left;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
            Invalidate();
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
