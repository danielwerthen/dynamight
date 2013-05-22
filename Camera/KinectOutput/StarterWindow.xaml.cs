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

namespace KinectOutput
{
    /// <summary>
    /// Interaction logic for StarterWindow.xaml
    /// </summary>
    public partial class StarterWindow : Window
    {
        public StarterWindow()
        {
            InitializeComponent();
        }

        private void SkeletonButton_Click(object sender, RoutedEventArgs e)
        {
            SkeletonWindow window = new SkeletonWindow();
            window.ShowDialog();
        }

        private void OutputToPngButton_Click(object sender, RoutedEventArgs e)
        {
            OutputToPngWindow window = new OutputToPngWindow();
            window.ShowDialog();
        }

        private void OverviewButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewWindow window = new OverviewWindow();
            window.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Default
            OverviewWindow window = new OverviewWindow();
            window.ShowDialog();
        }
    }
}
