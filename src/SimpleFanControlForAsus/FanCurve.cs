using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AsusFanControlGUI
{
    internal sealed class FanCurvePoint
    {
        public FanCurvePoint(int temperature, int fanSpeed)
        {
            Temperature = temperature;
            FanSpeed = fanSpeed;
        }

        public int Temperature { get; set; }
        public int FanSpeed { get; set; }
    }

    internal sealed class FanCurve
    {
        private const string DefaultCurve = "30:40;55:50;70:65;80:90;90:100";

        public FanCurve(IEnumerable<FanCurvePoint> points)
        {
            Points = points
                .OrderBy(point => point.Temperature)
                .Select(point => new FanCurvePoint(point.Temperature, point.FanSpeed))
                .ToList();

            if (Points.Count < 2)
                throw new ArgumentException("A fan curve requires at least two points.", nameof(points));
        }

        public List<FanCurvePoint> Points { get; }

        public static FanCurve CreateDefault()
        {
            return Parse(DefaultCurve);
        }

        public static FanCurve Parse(string value)
        {
            try
            {
                var points = (value ?? string.Empty)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Split(':'))
                    .Where(parts => parts.Length == 2)
                    .Select(parts => new FanCurvePoint(
                        int.Parse(parts[0], CultureInfo.InvariantCulture),
                        int.Parse(parts[1], CultureInfo.InvariantCulture)))
                    .Where(point => point.Temperature >= 30 && point.Temperature <= 100)
                    .Where(point => point.FanSpeed >= 0 && point.FanSpeed <= 100)
                    .OrderBy(point => point.Temperature)
                    .ToList();

                if (points.Count < 2)
                    throw new FormatException();

                for (var index = 1; index < points.Count; index++)
                {
                    if (points[index].Temperature <= points[index - 1].Temperature ||
                        points[index].FanSpeed < points[index - 1].FanSpeed)
                        throw new FormatException();
                }

                return new FanCurve(points);
            }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is OverflowException ||
                exception is ArgumentException)
            {
                if (string.Equals(value, DefaultCurve, StringComparison.Ordinal))
                    throw;

                return Parse(DefaultCurve);
            }
        }

        public int GetFanSpeed(double temperature)
        {
            if (temperature <= Points[0].Temperature)
                return Points[0].FanSpeed;

            var lastPoint = Points[Points.Count - 1];
            if (temperature >= lastPoint.Temperature)
                return lastPoint.FanSpeed;

            for (var index = 1; index < Points.Count; index++)
            {
                var right = Points[index];
                if (temperature > right.Temperature)
                    continue;

                var left = Points[index - 1];
                var position = (temperature - left.Temperature) /
                               (right.Temperature - left.Temperature);
                return (int)Math.Round(left.FanSpeed +
                    position * (right.FanSpeed - left.FanSpeed));
            }

            return lastPoint.FanSpeed;
        }

        public override string ToString()
        {
            return string.Join(";", Points.Select(point =>
                string.Format(CultureInfo.InvariantCulture, "{0}:{1}",
                    point.Temperature, point.FanSpeed)));
        }
    }
}
