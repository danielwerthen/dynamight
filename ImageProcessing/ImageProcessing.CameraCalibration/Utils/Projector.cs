﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{

    public class Projector
    {
        byte[] colorPixels;
        WriteableBitmap bitmap;

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
        internal static extern void MoveWindow(IntPtr hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        Window window;
        public Projector(int? screenIndex = null)
        {
            var screen = screenIndex.HasValue ?  Screen.AllScreens[screenIndex.Value] : Screen.AllScreens.First(row => !row.Primary);
            window = new Window();
            window.WindowState = WindowState.Maximized;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.Background = Brushes.Azure;
            window.Show();
            var id = GetForegroundWindow();
            MoveWindow(id, screen.Bounds.Left, 0, screen.Bounds.Width, screen.Bounds.Height, true);
            Image image;
            window.Content = image = new Image() { Stretch = System.Windows.Media.Stretch.Uniform, HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            var source = PresentationSource.FromVisual(window);
            var transform = source.CompositionTarget.TransformToDevice;
            var pixelSize = transform.Transform(new Point(window.ActualWidth, window.ActualHeight));
            bitmap = new WriteableBitmap((int)pixelSize.X, (int)pixelSize.Y, 96.0, 96.0, PixelFormats.Bgr24, null);
            image.Source = bitmap;
            colorPixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 3];
          
        }
        public System.Drawing.Size Size
        {
            get { return new System.Drawing.Size(1024, 768); }
        }

        public void Refresh()
        {
            window.InvalidateVisual();
        }
        public void DrawBackground()
        {
            DrawBackground(Colors.BlueViolet);
        }
        public void DrawBackground(Color color)
        {
            //A little bit messed up, dont worry!
            var c = new byte[] { color.R, color.B, color.G };
            int idx = 0;
            for (var i = 0; i < colorPixels.Length; i++)
                colorPixels[i] = c[++idx > 2 ? idx = 0 : idx];
            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), colorPixels, bitmap.PixelWidth * 3, 0);
            Refresh();
        }

        public void DrawScanLine(int step, int width, bool horizontal)
        {
            for (var y = 0; y < bitmap.PixelHeight; y++)
            {
                for (var x = 0; x < bitmap.PixelWidth; x++)
                {
                    byte intensity = (horizontal ? step * width > y && (step + 1) * width < y : step * width > y && (step + 1) * width < y) ? (byte)255 : (byte)0;
                    colorPixels[(x + y * bitmap.PixelHeight) * 3 + 0] = intensity;
                    colorPixels[(x + y * bitmap.PixelHeight) * 3 + 1] = intensity;
                    colorPixels[(x + y * bitmap.PixelHeight) * 3 + 2] = intensity;
                }
            }
            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), colorPixels, bitmap.PixelWidth * 3, 0);
            Refresh();
        }
        public void DrawPoints(System.Drawing.PointF[] corners, double size)
        {
            for (var y = 0; y < bitmap.PixelHeight; y++)
            {
                for (var x = 0; x < bitmap.PixelWidth; x++)
                {
                    byte intensity = corners.Any(row => Math.Abs(x - row.X) < size && Math.Abs(y - row.Y) < size) ? (byte)255 : (byte)0;
                    colorPixels[(x + y * bitmap.PixelHeight) * 3 + 0] = intensity;
                    colorPixels[(x + y * bitmap.PixelHeight) * 3 + 1] = intensity;
                    colorPixels[(x + y * bitmap.PixelHeight) * 3 + 2] = intensity;
                }
            }
            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), colorPixels, bitmap.PixelWidth * 3, 0);
            Refresh();
        }
    }

}