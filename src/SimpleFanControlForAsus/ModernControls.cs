using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class ToggleSwitch : CheckBox
    {
        private static readonly Color ActiveColor = Color.FromArgb(21, 112, 239);
        private static readonly Color InactiveColor = Color.FromArgb(190, 199, 210);

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            AutoSize = false;
            Size = new Size(46, 24);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? BackColor);

            var track = new Rectangle(0, 2, Width - 1, Height - 5);
            using (var path = CreateRoundedRectangle(track, track.Height / 2))
            using (var brush = new SolidBrush(Checked ? ActiveColor : InactiveColor))
                e.Graphics.FillPath(brush, path);

            var diameter = track.Height - 4;
            var x = Checked ? track.Right - diameter - 2 : track.Left + 2;
            using (var brush = new SolidBrush(Color.White))
                e.Graphics.FillEllipse(brush, x, track.Top + 2, diameter, diameter);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 90, 180);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 180);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class ModernSlider : Control
    {
        private int value = 90;
        private bool dragging;

        public ModernSlider()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.Selectable, true);
            Height = 34;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public event EventHandler ValueChanged;
        public event EventHandler ValueCommitted;

        public int Value
        {
            get { return value; }
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
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var centerY = Height / 2;
            var left = 9;
            var right = Math.Max(left + 1, Width - 9);
            var thumbX = left + (int)Math.Round((right - left) * Value / 100.0);

            using (var inactivePen = new Pen(Color.FromArgb(220, 226, 234), 6))
            using (var activePen = new Pen(Color.FromArgb(21, 112, 239), 6))
            {
                inactivePen.StartCap = inactivePen.EndCap = LineCap.Round;
                activePen.StartCap = activePen.EndCap = LineCap.Round;
                e.Graphics.DrawLine(inactivePen, left, centerY, right, centerY);
                e.Graphics.DrawLine(activePen, left, centerY, thumbX, centerY);
            }

            using (var shadowBrush = new SolidBrush(Color.FromArgb(35, 21, 112, 239)))
                e.Graphics.FillEllipse(shadowBrush, thumbX - 10, centerY - 10, 20, 20);
            using (var thumbBrush = new SolidBrush(Color.FromArgb(21, 112, 239)))
                e.Graphics.FillEllipse(thumbBrush, thumbX - 7, centerY - 7, 14, 14);
            using (var centerBrush = new SolidBrush(Color.White))
                e.Graphics.FillEllipse(centerBrush, thumbX - 3, centerY - 3, 6, 6);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
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
            if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
                return;

            Value += e.KeyCode == Keys.Right ? 1 : -1;
            ValueCommitted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void SetValueFromMouse(int x)
        {
            var left = 9;
            var width = Math.Max(1, Width - 18);
            Value = (int)Math.Round(
                Math.Max(0, Math.Min(1, (x - left) / (double)width)) * 100);
        }
    }

    internal sealed class ModernCard : Panel
    {
        public ModernCard()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Padding = new Padding(18);
            Margin = new Padding(0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(225, 231, 239)))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
