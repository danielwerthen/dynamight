using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Textures
{
    public class MultipleTextures
    {
        TextureProgram parent;
        public MultipleTextures(TextureProgram parent)
        {
            this.parent = parent;
        }

        private Func<Vector2, int, Vector2> transform;
        public Func<Vector2, int, Vector2> Transform
        {
            get { return transform; }
        }

        public void Load(Bitmap[] maps)
        {
            transform = BuildTransform(maps.Select(m => m.Size).ToArray());
            var bitmap = Merge(maps);
            this.parent.Resize(bitmap.Size);
            this.parent.LoadBitmap(bitmap);
        }

        public Func<Vector2, int, Vector2> BuildTransform(Size[] maps)
        {
            int width = (int)(Math.Ceiling(Math.Sqrt(maps.Length)));
            Size size = new Size(maps.Max(m => m.Width), maps.Max(m => m.Height));
            Size full = new Size(size.Width * width, size.Height * width);
            return (p, i) =>
            {
                int x = i % width;
                int y = (int)Math.Floor(i / (double)width);
                int xo = x * size.Width;
                int yo = y * size.Height;
                return new Vector2(
                        (xo + p.X * maps[i].Width) / (float)full.Width,
                        (yo + p.Y * maps[i].Height) / (float)full.Height
                    );
            };
        }

        public static Bitmap Merge(params Bitmap[] maps)
        {
            int width = (int)(Math.Ceiling(Math.Sqrt(maps.Length)));
            Size size = new Size(maps.Max(m => m.Width), maps.Max(m => m.Height));
            Bitmap result = new Bitmap(size.Width * width, size.Height * width, maps.First().PixelFormat);
            for (int i = 0; i < maps.Length; i++)
            {
                int x = i % width;
                int y = (int)Math.Floor(i / (double)width);
                Debug.Assert((x + y * width) == i);
                var map = maps[i];
                var toRead = map.LockBits(new Rectangle(0, 0, map.Width, map.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, map.PixelFormat);
                var toWrite = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, result.PixelFormat);

                int bytes = Math.Abs(toRead.Stride);
                byte[] buffer = new byte[bytes];
                int xo = x * size.Width;
                int yo = y * size.Height;
                for (var h = 0; h < toRead.Height; h++)
                {
                    Marshal.Copy(toRead.Scan0 + h * toRead.Stride, buffer, 0, bytes);
                    Marshal.Copy(buffer, 0, toWrite.Scan0 + xo * 4 + (yo + h) * toWrite.Stride, bytes);
                }
                
                map.UnlockBits(toRead);
                result.UnlockBits(toWrite);
            }
            return result;
        }
    }
}
