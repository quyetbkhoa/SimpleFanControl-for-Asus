using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class FanCurveControl : Control
    {
        private const int MinimumTemperature = 30;
        private const int MaximumTemperature = 100;
        private const int PointHitRadius = 22;
        private FanCurve curve = FanCurve.CreateDefault();
        private int draggedPointIndex = -1;
        private double? currentTemperature;
        public FanCurveControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Cursor = Cursors.Default;
            MinimumSize = new Size(280, 180);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public event EventHandler CurveChanged;
        public FanCurve Curve
        {
            get
            {
                return curve;
            }

            set
            {
                curve = value ?? FanCurve.CreateDefault();
                Invalidate();
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
                for (int speed = 0; speed <= 100; speed += 20)
                {
                    int y = (int)SpeedToY(speed, plot);
                    g.DrawLine(grid, plot.Left, y, plot.Right, y);
                    TextRenderer.DrawText(g, speed + "%", Font, new Rectangle(0, y - Scale(10), plot.Left - Scale(9), Scale(20)), UiPalette.Muted, flags | TextFormatFlags.Right);
                }

                for (int temperature = 30; temperature <= 100; temperature += 10)
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
                int x = (int)TemperatureToX(Math.Max(30, Math.Min(100, currentTemperature.Value)), plot);
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
                using (var fill = new SolidBrush(i == draggedPointIndex ? UiPalette.Orange : UiPalette.Primary))
                using (var ring = new Pen(Color.White, Scale(2)))
                {
                    g.FillEllipse(fill, vertex.X - radius, vertex.Y - radius, radius * 2, radius * 2);
                    g.DrawEllipse(ring, vertex.X - radius, vertex.Y - radius, radius * 2, radius * 2);
                }

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
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            draggedPointIndex = HitTestPoint(e.Location);
            if (draggedPointIndex < 0)
                return;
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
            var point = curve.Points[draggedPointIndex];
            var temperature = XToTemperature(e.X, plot);
            var fanSpeed = YToSpeed(e.Y, plot);
            var minimumTemperature = draggedPointIndex == 0 ? MinimumTemperature : curve.Points[draggedPointIndex - 1].Temperature + 1;
            var maximumTemperature = draggedPointIndex == curve.Points.Count - 1 ? MaximumTemperature : curve.Points[draggedPointIndex + 1].Temperature - 1;
            var minimumSpeed = draggedPointIndex == 0 ? 0 : curve.Points[draggedPointIndex - 1].FanSpeed;
            var maximumSpeed = draggedPointIndex == curve.Points.Count - 1 ? 100 : curve.Points[draggedPointIndex + 1].FanSpeed;
            point.Temperature = Math.Max(minimumTemperature, Math.Min(maximumTemperature, temperature));
            point.FanSpeed = Math.Max(minimumSpeed, Math.Min(maximumSpeed, fanSpeed));
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (draggedPointIndex < 0)
                return;
            draggedPointIndex = -1;
            Capture = false;
            Cursor = HitTestPoint(e.Location) >= 0 ? Cursors.Hand : Cursors.Default;
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
            for (var index = 0; index < curve.Points.Count; index++)
            {
                var point = curve.Points[index];
                var dx = location.X - TemperatureToX(point.Temperature, plot);
                var dy = location.Y - SpeedToY(point.FanSpeed, plot);
                if (dx * dx + dy * dy <= hitDistanceSquared)
                    return index;
            }

            return -1;
        }

        private Rectangle GetPlotRectangle()
        {
            return new Rectangle(Scale(44), Scale(44), Math.Max(1, Width - Scale(64)), Math.Max(1, Height - Scale(100)));
        }

        private static float TemperatureToX(double temperature, Rectangle plot)
        {
            return (float)(plot.Left + (temperature - MinimumTemperature) / (MaximumTemperature - MinimumTemperature) * plot.Width);
        }

        private static float SpeedToY(double speed, Rectangle plot)
        {
            return (float)(plot.Bottom - speed / 100.0 * plot.Height);
        }

        private static int XToTemperature(int x, Rectangle plot)
        {
            var position = (x - plot.Left) / (double)plot.Width;
            return (int)Math.Round(MinimumTemperature + Math.Max(0, Math.Min(1, position)) * (MaximumTemperature - MinimumTemperature));
        }

        private static int YToSpeed(int y, Rectangle plot)
        {
            var position = (plot.Bottom - y) / (double)plot.Height;
            return (int)Math.Round(Math.Max(0, Math.Min(1, position)) * 100);
        }
    }
}
