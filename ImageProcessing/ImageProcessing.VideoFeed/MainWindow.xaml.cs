using Dynamight.ImageProcessing.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageProcessing.VideoFeed
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Capture capture = new Capture(7);
            capture.Start();
            ComponentDispatcher.ThreadIdle += (o, arg) =>
                {
                    var img = capture.QueryFrame();
                    Emgu.CV.Contour<Bgr> con = new Contour<Bgr>(new MemStorage());
                    Display.Source = BitmapSourceConvert.ToBitmapSource(img);
                };
        }
    }
}
