using Microsoft.Kinect;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace CameraDrawing
{
    /// <summary>
    /// Interaction logic for StillCloseup.xaml
    /// </summary>
    public partial class StillCloseup : Window
    {
        public StillCloseup()
        {
            InitializeComponent();
            MinSlider.Minimum = 0;
            MinSlider.Maximum = short.MaxValue;
            MinSlider.Value = 0;
            MaxSlider.Minimum = 0;
            MaxSlider.Maximum = short.MaxValue;
            MaxSlider.Value = short.MaxValue;
        }

        public void LoadImage(WriteableBitmap bitmap, Action<WriteableBitmap, short, short> drawer)
        {
            Image.Source = bitmap;
            MinSlider.ValueChanged += (o, e) =>
            {
                drawer(bitmap, (short)MinSlider.Value, (short)MaxSlider.Value);
            };
            MaxSlider.ValueChanged += (o, e) =>
            {
                drawer(bitmap, (short)MinSlider.Value, (short)MaxSlider.Value);
            };
            drawer(bitmap, (short)MinSlider.Value, (short)MaxSlider.Value);
            SaveRangeButton.Click += (o, e) =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.AddExtension = true;
                sfd.FileName = DateTime.Today.ToShortTimeString();
                sfd.DefaultExt = "png";
                sfd.Filter = "Image files (*.png)|*.png";
                if (sfd.ShowDialog() == true)
                {
                    for (short i = 1000; i <= 18000; i += 500)
                    {
                        var clone = bitmap.Clone();
                        drawer(clone, (short)(i - 1000), i);
                        using (FileStream stream5 = new FileStream(System.IO.Path.GetDirectoryName(sfd.FileName) + "\\" + System.IO.Path.GetFileName(sfd.FileName) + i.ToString("00") + System.IO.Path.GetExtension(sfd.FileName), FileMode.Create))
                        {
                            PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                            encoder5.Frames.Add(BitmapFrame.Create(clone));
                            encoder5.Save(stream5);
                            stream5.Close();
                        }
                    }

                }
            };
        }
    }
}
