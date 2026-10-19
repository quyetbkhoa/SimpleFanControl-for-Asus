using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class FanCurveControl : Control
    {
        private const int PointHitRadius = 22;
        private FanCurve curve = FanCurve.CreateDefault();
        private int draggedPointIndex = -1;
        private int selectedPointIndex;
        private double? currentTemperature;
        private PointF? dragVisual;
        private PointF dragOffset;
        private int dragMinimum, dragMaximum;
        public bool IsDragging
        {
            get
            {
                return draggedPointIndex >= 0;
            }
        }

        private int PlotMinimum
        {
            get
            {
                return IsDragging ? dragMinimum : Math.Min(30, curve.Points[0].Temperature) / 10 * 10;
            }
        }

        private int PlotMaximum
        {
            get
            {
                return IsDragging ? dragMaximum : Math.Max(100, (curve.Points[curve.Points.Count - 1].Temperature + 9) / 10 * 10);
            }
        }

        public void AddPoint(bool atStart)
        {
            int index = curve.AddEndpoint(atStart);
            if (index < 0)
                return;
            SelectedPointIndex = index;
            Invalidate();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        public FanCurveControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Cursor = Cursors.Default;
            MinimumSize = new Size(280, 140);
            TabStop = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        }

        public event EventHandler CurveChanged;
        public event EventHandler SelectedPointChanged;
        public int SelectedPointIndex
        {
            get
            {
                return selectedPointIndex;
            }

            set
            {
                int index = Math.Max(0, Math.Min(curve.Points.Count - 1, value));
                if (selectedPointIndex == index)
                    return;
                selectedPointIndex = index;
                Invalidate();
                SelectedPointChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdatePoint(int index, int temperature, int speed)
        {
            if (index < 0 || index >= curve.Points.Count)
                return;
            curve.MovePoint(index, temperature, speed);
            SelectedPointIndex = index;
            Invalidate();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        public FanCurve Curve
        {
            get
            {
                return curve;
            }

            set
            {
                curve = value ?? FanCurve.CreateDefault();
                selectedPointIndex = Math.Min(selectedPointIndex, curve.Points.Count - 1);
                Invalidate();
                SelectedPointChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double? CurrentTemperature
        {
            get
            {
                return currentTemperature;
            }

            set
            {
                currentTemperature = value;
                Invalidate();
            }
        }

        public string TemperatureAxisTitle { get; set; } = "CPU temperature (°C)";
        public string SpeedAxisTitle { get; set; } = "Fan speed (%)";

        private int Scale(int value)
        {
            return Math.Max(1, (int)Math.Round(value * DeviceDpi / 96f));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            DrawingHelper.Configure(g);
            var plot = GetPlotRectangle();
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            using (var grid = new Pen(UiPalette.Border))
            {
                int speedStep = plot.Height < Scale(120) ? 50 : 20;
                for (int speed = 0; speed <= 100; speed += speedStep)
                {
                    int y = (int)SpeedToY(speed, plot);
                    g.DrawLine(grid, plot.Left, y, plot.Right, y);
                    TextRenderer.DrawText(g, speed + "%", Font, new Rectangle(0, y - Scale(10), plot.Left - Scale(9), Scale(20)), UiPalette.Muted, flags | TextFormatFlags.Right);
                }

                for (int temperature = PlotMinimum; temperature <= PlotMaximum; temperature += 10)
                {
                    int x = (int)TemperatureToX(temperature, plot);
                    g.DrawLine(grid, x, plot.Top, x, plot.Bottom);
                    TextRenderer.DrawText(g, temperature + "°", Font, new Rectangle(x - Scale(18), plot.Bottom + Scale(8), Scale(36), Scale(20)), UiPalette.Muted, flags | TextFormatFlags.HorizontalCenter);
                }
            }

            TextRenderer.DrawText(g, SpeedAxisTitle, Font, new Rectangle(plot.Left, 0, plot.Width, Scale(24)), UiPalette.Muted, flags);
            TextRenderer.DrawText(g, TemperatureAxisTitle, Font, new Rectangle(plot.Left, Height - Scale(24), plot.Width, Scale(22)), UiPalette.Muted, flags | TextFormatFlags.HorizontalCenter);
            var vertices = new PointF[curve.Points.Count];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = new PointF(TemperatureToX(curve.Points[i].Temperature, plot), SpeedToY(curve.Points[i].FanSpeed, plot));
            if (dragVisual.HasValue && draggedPointIndex >= 0)
                vertices[draggedPointIndex] = new PointF(TemperatureToX(dragVisual.Value.X, plot), SpeedToY(dragVisual.Value.Y, plot));
            using (var area = new GraphicsPath())
            using (var fill = new LinearGradientBrush(plot, Color.FromArgb(40, UiPalette.Primary), Color.FromArgb(3, UiPalette.Primary), LinearGradientMode.Vertical))
            using (var line = new Pen(UiPalette.Primary, Scale(3)))
            {
                area.AddLine(plot.Left, vertices[0].Y, vertices[0].X, vertices[0].Y);
                area.AddLines(vertices);
                area.AddLine(vertices[vertices.Length - 1], new PointF(plot.Right, vertices[vertices.Length - 1].Y));
                area.AddLine(plot.Right, vertices[vertices.Length - 1].Y, plot.Right, plot.Bottom);
                area.AddLine(plot.Right, plot.Bottom, plot.Left, plot.Bottom);
                area.CloseFigure();
                g.FillPath(fill, area);
                g.DrawLines(line, vertices);
                g.DrawLine(line, new PointF(plot.Left, vertices[0].Y), vertices[0]);
                g.DrawLine(line, vertices[vertices.Length - 1], new PointF(plot.Right, vertices[vertices.Length - 1].Y));
            }

            if (currentTemperature.HasValue)
            {
                int x = (int)TemperatureToX(Math.Max(PlotMinimum, Math.Min(PlotMaximum, currentTemperature.Value)), plot);
                using (var marker = new Pen(UiPalette.Orange, Scale(2)))
                {
                    marker.DashStyle = DashStyle.Dash;
                    g.DrawLine(marker, x, plot.Top, x, plot.Bottom);
                }

                var badge = new Rectangle(Math.Max(plot.Left, Math.Min(plot.Right - Scale(52), x - Scale(26))), plot.Top - Scale(27), Scale(52), Scale(21));
                using (var shape = DrawingHelper.RoundedRectangle(badge, Scale(5)))
                using (var fill = new SolidBrush(Color.FromArgb(255, 242, 225)))
                    g.FillPath(fill, shape);
                TextRenderer.DrawText(g, currentTemperature.Value.ToString("0") + "°C", Font, badge, Color.FromArgb(167, 87, 17), flags | TextFormatFlags.HorizontalCenter);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                int radius = Scale(6);
                using (var fill = new SolidBrush(UiPalette.Primary))
                using (var ring = new Pen(Color.White, Scale(2)))
                {
                    g.FillEllipse(fill, vertex.X - radius, vertex.Y - radius, radius * 2, radius * 2);
                    g.DrawEllipse(ring, vertex.X - radius, vertex.Y - radius, radius * 2, radius * 2);
                }

                if (i == selectedPointIndex)
                    using (var selection = new Pen(UiPalette.PrimaryDark, Scale(1)))
                        g.DrawEllipse(selection, vertex.X - radius - Scale(3), vertex.Y - radius - Scale(3), (radius + Scale(3)) * 2, (radius + Scale(3)) * 2);
                if (curve.Points.Count > 10 && i != selectedPointIndex && i != 0 && i != curve.Points.Count - 1)
                    continue;
                string label = curve.Points[i].Temperature + "° · " + curve.Points[i].FanSpeed + "%";
                var size = TextRenderer.MeasureText(g, label, Font, new Size(int.MaxValue, int.MaxValue), flags);
                int x = Math.Max(plot.Left, Math.Min(plot.Right - size.Width, (int)vertex.X - size.Width / 2));
                int y = (int)vertex.Y - Scale(25);
                if (y < plot.Top)
                    y = (int)vertex.Y + Scale(12);
                var bounds = new Rectangle(x - Scale(3), y, size.Width + Scale(6), Scale(21));
                using (var fill = new SolidBrush(Color.FromArgb(246, 250, 255)))
                    g.FillRectangle(fill, bounds);
                TextRenderer.DrawText(g, label, Font, bounds, UiPalette.PrimaryDark, flags | TextFormatFlags.HorizontalCenter);
            }

            if (Focused)
                ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -2, -2), UiPalette.PrimaryDark, BackColor);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            int minimum = PlotMinimum, maximum = PlotMaximum;
            draggedPointIndex = HitTestPoint(e.Location);
            if (draggedPointIndex < 0)
                return;
            dragMinimum = minimum;
            dragMaximum = maximum;
            SelectedPointIndex = draggedPointIndex;
            Focus();
            Cursor = Cursors.SizeAll;
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (draggedPointIndex < 0 || !Capture)
            {
                Cursor = HitTestPoint(e.Location) >= 0 ? Cursors.Hand : Cursors.Default;
                return;
            }

            var plot = GetPlotRectangle();
            double min = draggedPointIndex == 0 ? 0 : curve.Points[draggedPointIndex - 1].Temperature + 1;
            double max = draggedPointIndex == curve.Points.Count - 1 ? 110 : curve.Points[draggedPointIndex + 1].Temperature - 1;
            double temperature = PlotMinimum + ((e.X + dragOffset.X - plot.Left) / plot.Width) * (PlotMaximum - PlotMinimum);
            double speed = (plot.Bottom - e.Y - dragOffset.Y) / plot.Height * 100.0;
            temperature = Math.Max(Math.Max(PlotMinimum, min), Math.Min(Math.Min(PlotMaximum, max), temperature));
            speed = Math.Max(0, Math.Min(100, speed));
            dragVisual = new PointF((float)temperature, (float)speed);
            curve.MovePoint(draggedPointIndex, (int)Math.Round(temperature), (int)Math.Round(speed));
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
                SelectedPointIndex += e.KeyCode == Keys.Right ? 1 : -1;
            else
            {
                var point = curve.Points[selectedPointIndex];
                if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down)
                    return;
                UpdatePoint(selectedPointIndex, point.Temperature + (e.KeyCode == Keys.Left ? -1 : e.KeyCode == Keys.Right ? 1 : 0), point.FanSpeed + (e.KeyCode == Keys.Down ? -1 : e.KeyCode == Keys.Up ? 1 : 0));
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                int index = HitTestPoint(e.Location);
                if (index < 0)
                    return;
                SelectedPointIndex = index;
                using (var menu = new ContextMenu(new[] { new MenuItem(TemperatureAxisTitle + " -1", (a, b) => UpdatePoint(index, curve.Points[index].Temperature - 1, curve.Points[index].FanSpeed)), new MenuItem(TemperatureAxisTitle + " +1", (a, b) => UpdatePoint(index, curve.Points[index].Temperature + 1, curve.Points[index].FanSpeed)), new MenuItem(SpeedAxisTitle + " -1", (a, b) => UpdatePoint(index, curve.Points[index].Temperature, curve.Points[index].FanSpeed - 1)), new MenuItem(SpeedAxisTitle + " +1", (a, b) => UpdatePoint(index, curve.Points[index].Temperature, curve.Points[index].FanSpeed + 1)) }))
                    menu.Show(this, e.Location);
                return;
            }

            if (draggedPointIndex < 0)
                return;
            draggedPointIndex = -1;
            Capture = false;
            Cursor = HitTestPoint(e.Location) >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (Capture || draggedPointIndex < 0)
                return;
            draggedPointIndex = -1;
            dragVisual = null;
            Invalidate();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!Capture)
                Cursor = Cursors.Default;
        }

        private int HitTestPoint(Point location)
        {
            var plot = GetPlotRectangle();
            var radius = Scale(PointHitRadius);
            var hitDistanceSquared = radius * radius;
            int nearest = -1;
            for (var index = 0; index < curve.Points.Count; index++)
            {
                var point = curve.Points[index];
                var dx = location.X - TemperatureToX(point.Temperature, plot);
                var dy = location.Y - SpeedToY(point.FanSpeed, plot);
                float distance = dx * dx + dy * dy;
                if (distance <= hitDistanceSquared)
                {
                    nearest = index;
                    hitDistanceSquared = (int)distance;
                }
            }

            return nearest;
        }

        private Rectangle GetPlotRectangle()
        {
            return new Rectangle(Scale(44), Scale(44), Math.Max(1, Width - Scale(64)), Math.Max(1, Height - Scale(100)));
        }

        private float TemperatureToX(double temperature, Rectangle plot)
        {
            return (float)(plot.Left + (temperature - PlotMinimum) / (PlotMaximum - PlotMinimum) * plot.Width);
        }

        private static float SpeedToY(double speed, Rectangle plot)
        {
            return (float)(plot.Bottom - speed / 100.0 * plot.Height);
        }

        private int XToTemperature(int x, Rectangle plot)
        {
            var position = (x - plot.Left) / (double)plot.Width;
            return (int)Math.Round(PlotMinimum + Math.Max(0, Math.Min(1, position)) * (PlotMaximum - PlotMinimum));
        }

        private static int YToSpeed(int y, Rectangle plot)
        {
            var position = (plot.Bottom - y) / (double)plot.Height;
            return (int)Math.Round(Math.Max(0, Math.Min(1, position)) * 100);
        }
    }
}
