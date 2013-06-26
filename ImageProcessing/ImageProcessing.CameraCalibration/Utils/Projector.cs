using Graphics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
    public class Projector
    {
        public BitmapWindow window;
        public Bitmap bitmap;
        public Projector()
        {
            var display = DisplayDevice.AvailableDisplays.First(row => !row.IsPrimary);
            window = new BitmapWindow(display.Bounds.Left, display.Bounds.Top, display.Width, display.Height, display);
            window.Fullscreen = true;
            window.Load();
            window.ResizeGraphics();
            bitmap = new Bitmap(window.Width, window.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            window.LoadBitmap(bitmap);
        }

        public void SetBounds(RectangleF bounds)
        {
            window.SetBounds(bounds);
        }

        public void DrawBackground()
        {
            DrawBackground(Color.Black);
        }

        public void DrawBackground(Color color)
        {
            QuickDraw.Start(bitmap)
                .Fill(color)
                .Finish();
            window.LoadBitmap(bitmap);
            window.RenderFrame();
        }

        public void DrawBitmap(Bitmap bitmap)
        {
            window.DrawBitmap(bitmap);
        }

        public void DrawBinary(int step, bool vertical, Color color)
        {
            QuickDraw.Start(bitmap)
                .All((x,y) =>
                    {
                        double intensity = 0;
                        double dt;
                        if (vertical)
                        {
                            dt = (x / (double)bitmap.Width);
                        }
                        else
                            dt = (y / (double)bitmap.Height);
                        intensity = Math.Floor(dt * Math.Pow(2, step)) % 2;
                        return Color.FromArgb((byte)(intensity*color.R), (byte)(intensity * color.G), (byte)(intensity * color.B));
                    }, false)
                .Finish();
            window.LoadBitmap(bitmap);
            window.RenderFrame();
        }

        public void DrawScanLine(int Step, int Length, bool Rows)
        {
            QuickDraw.Start(bitmap)
                .Fill(Color.Black)
                .DrawShape(new System.Windows.Media.RectangleGeometry(new Rect(!Rows ? Step * Length : 0, Rows ? Step * Length : 0, !Rows ? Length : bitmap.Width, Rows ? Length : bitmap.Height)))
                .Finish();
            window.LoadBitmap(bitmap);
            window.RenderFrame();
        }

        public void DrawPoints(System.Drawing.PointF[] points, float size)
        {
            var draw = QuickDraw.Start(bitmap)
                .Fill(Color.Black);
            foreach (var p in points)
                draw.DrawPoint(p.X, p.Y, size);
            draw.Finish();
            window.LoadBitmap(bitmap);
            window.RenderFrame();
        }

        public System.Drawing.Size Size
        {
            get { return window.Size; }
        }
    }

}
