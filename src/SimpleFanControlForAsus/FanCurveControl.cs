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
        private const int MarginLeft = 42;
        private const int MarginTop = 14;
        private const int MarginRight = 16;
        private const int MarginBottom = 32;
        private const int PointRadius = 8;
        private const int PointHitRadius = 22;

        private FanCurve curve = FanCurve.CreateDefault();
        private int draggedPointIndex = -1;
        private double? currentTemperature;

        public FanCurveControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Cursor = Cursors.Default;
            MinimumSize = new Size(300, 180);
        }

        public event EventHandler CurveChanged;

        public FanCurve Curve
        {
            get { return curve; }
            set
            {
                curve = value ?? FanCurve.CreateDefault();
                Invalidate();
            }
        }

        public double? CurrentTemperature
        {
            get { return currentTemperature; }
            set
            {
                currentTemperature = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var plot = GetPlotRectangle();

            using (var gridPen = new Pen(Color.FromArgb(225, 230, 235)))
            using (var textBrush = new SolidBrush(Color.FromArgb(80, 85, 90)))
            {
                for (var speed = 0; speed <= 100; speed += 20)
                {
                    var y = SpeedToY(speed, plot);
                    graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    var label = speed + "%";
                    var size = graphics.MeasureString(label, Font);
                    graphics.DrawString(label, Font, textBrush,
                        plot.Left - size.Width - 5, y - size.Height / 2);
                }

                for (var temperature = 40; temperature <= 100; temperature += 10)
                {
                    var x = TemperatureToX(temperature, plot);
                    graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                    var label = temperature + "°";
                    var size = graphics.MeasureString(label, Font);
                    graphics.DrawString(label, Font, textBrush,
                        x - size.Width / 2, plot.Bottom + 6);
                }
            }

            if (currentTemperature.HasValue)
            {
                var temperature = Math.Max(MinimumTemperature,
                    Math.Min(MaximumTemperature, currentTemperature.Value));
                var x = TemperatureToX(temperature, plot);
                using (var temperaturePen = new Pen(Color.FromArgb(230, 140, 25), 2))
                {
                    temperaturePen.DashStyle = DashStyle.Dash;
                    graphics.DrawLine(temperaturePen, x, plot.Top, x, plot.Bottom);
                }
            }

            using (var linePen = new Pen(Color.FromArgb(35, 110, 190), 3))
            using (var pointBrush = new SolidBrush(Color.FromArgb(35, 110, 190)))
            using (var selectedBrush = new SolidBrush(Color.FromArgb(230, 140, 25)))
            using (var pointTextBrush = new SolidBrush(Color.FromArgb(45, 50, 55)))
            {
                for (var index = 1; index < curve.Points.Count; index++)
                {
                    var previous = curve.Points[index - 1];
                    var point = curve.Points[index];
                    graphics.DrawLine(linePen,
                        TemperatureToX(previous.Temperature, plot),
                        SpeedToY(previous.FanSpeed, plot),
                        TemperatureToX(point.Temperature, plot),
                        SpeedToY(point.FanSpeed, plot));
                }

                for (var index = 0; index < curve.Points.Count; index++)
                {
                    var point = curve.Points[index];
                    var x = TemperatureToX(point.Temperature, plot);
                    var y = SpeedToY(point.FanSpeed, plot);
                    graphics.FillEllipse(index == draggedPointIndex ? selectedBrush : pointBrush,
                        x - PointRadius, y - PointRadius,
                        PointRadius * 2, PointRadius * 2);

                    var label = point.Temperature + "°/" + point.FanSpeed + "%";
                    var labelSize = graphics.MeasureString(label, Font);
                    var labelX = Math.Max(plot.Left,
                        Math.Min(plot.Right - labelSize.Width, x - labelSize.Width / 2));
                    var labelY = y - labelSize.Height - 7;
                    if (labelY < plot.Top)
                        labelY = y + 7;
                    graphics.DrawString(label, Font, pointTextBrush, labelX, labelY);
                }
            }

            using (var borderPen = new Pen(Color.FromArgb(150, 155, 160)))
                graphics.DrawRectangle(borderPen, plot);
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
                Cursor = HitTestPoint(e.Location) >= 0
                    ? Cursors.Hand
                    : Cursors.Default;
                return;
            }

            var plot = GetPlotRectangle();
            var point = curve.Points[draggedPointIndex];
            var temperature = XToTemperature(e.X, plot);
            var fanSpeed = YToSpeed(e.Y, plot);

            var minimumTemperature = draggedPointIndex == 0
                ? MinimumTemperature
                : curve.Points[draggedPointIndex - 1].Temperature + 1;
            var maximumTemperature = draggedPointIndex == curve.Points.Count - 1
                ? MaximumTemperature
                : curve.Points[draggedPointIndex + 1].Temperature - 1;
            var minimumSpeed = draggedPointIndex == 0
                ? 0
                : curve.Points[draggedPointIndex - 1].FanSpeed;
            var maximumSpeed = draggedPointIndex == curve.Points.Count - 1
                ? 100
                : curve.Points[draggedPointIndex + 1].FanSpeed;

            point.Temperature = Math.Max(minimumTemperature,
                Math.Min(maximumTemperature, temperature));
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
            Cursor = HitTestPoint(e.Location) >= 0
                ? Cursors.Hand
                : Cursors.Default;
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
            var hitDistanceSquared = PointHitRadius * PointHitRadius;

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
            return new Rectangle(
                MarginLeft,
                MarginTop,
                Math.Max(1, Width - MarginLeft - MarginRight),
                Math.Max(1, Height - MarginTop - MarginBottom));
        }

        private static float TemperatureToX(double temperature, Rectangle plot)
        {
            return (float)(plot.Left +
                (temperature - MinimumTemperature) /
                (MaximumTemperature - MinimumTemperature) * plot.Width);
        }

        private static float SpeedToY(double speed, Rectangle plot)
        {
            return (float)(plot.Bottom - speed / 100.0 * plot.Height);
        }

        private static int XToTemperature(int x, Rectangle plot)
        {
            var position = (x - plot.Left) / (double)plot.Width;
            return (int)Math.Round(MinimumTemperature +
                Math.Max(0, Math.Min(1, position)) *
                (MaximumTemperature - MinimumTemperature));
        }

        private static int YToSpeed(int y, Rectangle plot)
        {
            var position = (plot.Bottom - y) / (double)plot.Height;
            return (int)Math.Round(Math.Max(0, Math.Min(1, position)) * 100);
        }
    }
}
