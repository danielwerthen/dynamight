using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    /// <summary>
    /// Interaction logic for StructuredLightWindow.xaml
    /// </summary>
    public partial class StructuredLightWindow : Window
    {
        DrawingGroup drawingGroup;
        int currentStep = 0;
        bool rows = false;

        public StructuredLightWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (this.WindowState == System.Windows.WindowState.Normal)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                    this.WindowStyle = System.Windows.WindowStyle.None;
                    Hide();
                    Show();
                }
                else
                {
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                }
            }
            else
                Draw();
        }

        private void Display_Loaded(object sender, RoutedEventArgs e)
        {
            drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            Display.Source = imageSource;
        }

        public Action<System.Drawing.Bitmap> Interpret(out Func<List<System.Drawing.Point>, List<System.Drawing.Point>> transformPoints, Action complete)
        {
            
            return StructuredLightInterpreter.Build(() =>
                {
                    using (var dc = drawingGroup.Open())
                        dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, DisplayGrid.ActualWidth, DisplayGrid.ActualHeight));
                }, () =>
                {
                    var record = new Dynamight.ImageProcessing.CameraCalibration.StructuredLightInterpreter.MapRecord()
                    {
                        Pixels = rows ? (int)DisplayGrid.ActualWidth : (int)DisplayGrid.ActualHeight,
                        Step = currentStep,
                        Row = rows
                    };
                    var prevRows = rows;
                    Draw();
                    if (prevRows == true && !rows)
                    {
                        complete();
                    }
                    return record;
                }, out transformPoints);
        }

        private void Draw()
        {
            DrawPhased(currentStep++ - 1, rows);
            if (currentStep > 2)
            {
                currentStep = 0;
                rows = !rows;
            }
            return;
            Draw(currentStep++);
            if (Math.Ceiling(Math.Log(this.ActualWidth, 2)) < currentStep)
            {
                currentStep = 0;
                rows = !rows;
            }
        }

        private void Draw(int step)
        {
            bool everyOther = true;
            Brush white = Brushes.Blue;
            Func<Brush> binaryBrush = () => (everyOther = !everyOther) ? Brushes.Black : white;
            using (var dc = drawingGroup.Open())
            {
                if (!rows)
                {
                    var width = DisplayGrid.ActualWidth;
                    var height = DisplayGrid.ActualHeight;
                    var steps = (new double[] { 0 }).Concat(GetBinarySteps(step)).Concat(new double[] { 1 }).Select(row => row * width).ToArray();
                    for (var i = 0; i < steps.Length - 1; i++)
                    {
                        //Brush brush = (everyOther = !everyOther) ? (Brush)Brushes.Black :
                        //    new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(Colors.Black, 0), new GradientStop(Colors.White, 0.5), new GradientStop(Colors.Black,1) }), 0);
                            dc.DrawRectangle(binaryBrush(), null, new Rect(new Point(steps[i], 0), new Size(steps[i+1], height)));
                    }
                }
                else
                {
                    var width = DisplayGrid.ActualWidth;
                    var height = DisplayGrid.ActualHeight;
                    var steps = (new double[] { 0 }).Concat(GetBinarySteps(step)).Concat(new double[] { 1 }).Select(row => row * height).ToArray();
                    for (var i = 0; i < steps.Length - 1; i++)
                    {
                        dc.DrawRectangle(binaryBrush(), null, new Rect(new Point(0, steps[i]), new Size(width, steps[i + 1])));
                    }
                }
            }
        }

        private void DrawPhased(int step, bool row, double tlength = 2 * Math.PI, double phaseStep = Math.PI * 2 / 3)
        {
            WriteableBitmap wbitmap = new WriteableBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            DisplayPhase.Source = wbitmap;
            Display.Visibility = System.Windows.Visibility.Hidden;
            var colorPixels = new byte[wbitmap.PixelWidth * wbitmap.PixelHeight * sizeof(int)];
            for (var y = 0; y < wbitmap.PixelHeight; y++)
            {
                for (var x = 0; x < wbitmap.PixelWidth; x++)
                {
                    double dt = row ? (double)x / wbitmap.PixelWidth : (double)y / wbitmap.PixelHeight;
                    double theta = dt * tlength;
                    var res = (byte)((Math.Cos(theta + step * phaseStep) * 0.25 + 0.5) * 255);
                    int idx = (x + y * wbitmap.PixelWidth) * 4;
                    colorPixels[idx + 0] = res;
                    colorPixels[idx + 1] = res;
                    colorPixels[idx + 2] = res;
                    colorPixels[idx + 3] = 255;
                }
            }
            wbitmap.WritePixels(new Int32Rect(0, 0, wbitmap.PixelWidth, wbitmap.PixelHeight), colorPixels, wbitmap.PixelWidth * sizeof(int), 0);
        }

        private IEnumerable<double> GetBinarySteps(int count, double rangeStart = 0, double rangeEnd = 1.0)
        {
            double mid = rangeStart + (rangeEnd - rangeStart) / 2;
            if (count == 0)
                yield return rangeEnd;
            else if (count == 1)
            {
                yield return mid;
            }
            else
                foreach (var value in GetBinarySteps(count - 1, rangeStart, mid).Concat(new double[] { mid }).Concat(GetBinarySteps(count - 1, mid, rangeEnd)))
                    yield return value;
        }
    }
}
