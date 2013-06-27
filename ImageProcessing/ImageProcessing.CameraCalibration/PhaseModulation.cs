using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public class PhaseModulation
    {
        public static double PhaseToIntensity(double dt, int step, double phase)
        {
            return Math.Cos((dt - 0.5 / step) * Math.PI * step + phase);
        }

        public static double IntensityToPhase(double i1, double i2, double i3)
        {
            return (Math.Atan(Math.Sqrt(3) * (i1 - i3) / (2 * i2 - i1 - i3))
                / Math.PI) + 0.5;
        }

        public static double AbsolutePhase(double[] phases, int[] steps)
        {
            double ap = 0;
            for (var i = 0; i < phases.Count(); i++)
            {
                ap = Math.Floor(ap / (1.0 / (double)steps[i])) * (1.0 / (double)steps[i]) + (phases[i] < 1 ? phases[i] / steps[i] : 0);
                ap = Math.Round(ap, 5);
            }
            return ap;
        }

        public static void DrawPhaseModulation(int step, double phase, bool vertical, Color color, Bitmap bitmap)
        {
            var w = bitmap.Width;
            var h = bitmap.Height;
            Graphics.QuickDraw.Start(bitmap)
                .All((x, y) =>
                    {
                        var intensity = 0.0;
                        double dt;
                        if (vertical)
                            dt = x / (double)w;
                        else
                            dt = y / (double)h;
                        intensity = (PhaseToIntensity(dt, step, phase) + 1.0) / 4.0 + 0.5;
                        return Color.FromArgb((int)(intensity * color.R), (int)(intensity * color.G), (int)(intensity * color.B));
                    }, false)
                .Finish();
        }

        public static Action<Bitmap> DrawPhaseModulation(int step, double phase, bool vertical, Color color)
        {
            return (bitmap) =>
                {
                    DrawPhaseModulation(step, phase, vertical, color, bitmap);
                };
        }

        private static double Clamp(double d, double min, double max)
        {
            if (d < min)
                return min;
            if (d > max)
                return max;
            return d;
        }

        public static double DetermineAlgorithmIntegrity(int steps = 5, int pixelCount = 10000, double noiselvl = 0.01)
        {
            double[][] tss = new double[steps][];
            var ids = new int[pixelCount];
            int idx = 0;
            ids = ids.Select(row => (int)idx++).ToArray();
            var xs = ids.Select(row => (double)row / (double)ids.Length).ToArray();
            var stepArray = new int[steps];
            for (var i = 1; i <= steps; i++)
            {
                stepArray[i - 1] = i;
                var r = new Random(123);
                var r1 = ids.Select(row => r.NextDouble() * noiselvl).ToArray();
                var r2 = ids.Select(row => r.NextDouble() * noiselvl).ToArray();
                var r3 = ids.Select(row => r.NextDouble() * noiselvl).ToArray();
                var x1 = ids.Select(row => PhaseToIntensity(xs[row], i, -2 * Math.PI / 3) + r1[row]).Select(row => Clamp(row, -1, 1)).ToArray();
                var x2 = ids.Select(row => PhaseToIntensity(xs[row], i, 0) + r2[row]).Select(row => Clamp(row, -1, 1)).ToArray();
                var x3 = ids.Select(row => PhaseToIntensity(xs[row], i, 2 * Math.PI / 3) + r3[row]).Select(row => Clamp(row, -1, 1)).ToArray();

                tss[i - 1] = ids.Select(ii => IntensityToPhase(x1[ii], x2[ii], x3[ii]))
                    .ToArray();
            }

            var aps = ids.Select(i => AbsolutePhase(stepArray.Select(row => tss[row - 1][i]).ToArray(), stepArray)).ToArray();
            var aperr = ids.Select(i => Math.Abs(aps[i] - xs[i])).ToArray();
            var aperrtot = aperr.Sum() / aps.Count();
            return aperrtot / noiselvl;
        }
    }
}
