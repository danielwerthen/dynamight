using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class FastBitmap : IDisposable
	{
		Bitmap bitmap;
		int width;
		int height;
		BitmapData data;
		public FastBitmap(Bitmap bitmap)
		{
			this.bitmap = bitmap;
			width = bitmap.Width;
			height = bitmap.Height;
			data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
		}

		public void Fill()
		{
			//Fill(new byte[width * height * 4]);
		}

		public void Fill(ref byte[] source)
		{
			Marshal.Copy(source, 0, data.Scan0, source.Length);
		}

		public Color this[int x, int y]
		{
			set
			{
				if (x < 0 || x >= width)
					return;
				if (y < 0 || y >= height)
					return;
				unsafe
				{
					
					byte* row = (byte*)data.Scan0 + (y * data.Stride);
					int size = (Image.GetPixelFormatSize(bitmap.PixelFormat) / 8);
					var idx = x * size;
					row[idx + 0] = value.R;
					row[idx + 1] = value.G;
					row[idx + 2] = value.B;
					if (size > 3)
						row[idx + 3] = value.A;
				}
			}
		}

		public byte this[int x, int y, int ch]
		{
			set
			{
				if (x < 0 || x >= width)
					return;
				if (y < 0 || y >= height)
					return;
				unsafe
				{
					byte* row = (byte*)data.Scan0 + (y * data.Stride);
					row[x * (Image.GetPixelFormatSize(bitmap.PixelFormat) / 8) + ch] = value;
				}
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			bitmap.UnlockBits(data);
		}

		#endregion
	}
}
