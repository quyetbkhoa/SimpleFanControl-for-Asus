using System;

namespace AsusFanControlGUI
{
    internal sealed class FanSpeedRamp
    {
        private double current;
        public void Reset(int value)
        {
            current = value;
        }

        public int Next(int target, double elapsedSeconds, double risePerSecond, double fallPerSecond, bool enabled, bool urgent)
        {
            if (target == 0 || current == 0 || !enabled || (urgent && target > current))
                current = target;
            else
            {
                double step = Math.Max(0, elapsedSeconds) * (target > current ? risePerSecond : fallPerSecond);
                current += Math.Sign(target - current) * Math.Min(Math.Abs(target - current), step);
            }

            return (int)Math.Round(current);
        }
    }
}
