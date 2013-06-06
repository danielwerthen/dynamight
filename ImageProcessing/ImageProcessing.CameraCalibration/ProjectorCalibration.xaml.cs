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
    /// Interaction logic for ProjectorCalibration.xaml
    /// </summary>
    public partial class ProjectorCalibration : Window
    {
        public ProjectorCalibration()
        {
            InitializeComponent();
        }

        private DrawingGroup drawingGroup;
        private void Display_Loaded(object sender, RoutedEventArgs e)
        {
            drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            Display.Source = imageSource;
            Redraw();
        }

        private void Redraw()
        {
            Draw(7,7, new Size(this.ActualWidth, this.ActualHeight));
        }

        private void Draw(int widthCount, int heightCount, Size size)
        {
            if (Display.Source == null)
                return;
            Func<Brush> checkerBrush;
            Func<Brush> black = null;
            Func<Brush> white = null;
            black = () => { 
                checkerBrush = white;
                return Brushes.Black;
            };
            white = () => { 
                checkerBrush = black;
                return Brushes.White;
            };
            checkerBrush = black;
            using (var dc = drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0,0, size.Width, size.Height));
                double dw = size.Width / (double)widthCount;
                double dh = size.Height / (double)heightCount;
                for (var y = 0; y < heightCount; y++)
                {
                    for (var x = 0; x < widthCount; x++)
                    {
                        dc.DrawRectangle(checkerBrush(), null, new Rect(x * dw, y * dh, dw, dh));
                    }
                }
            }
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Redraw();
        }
    }
}
