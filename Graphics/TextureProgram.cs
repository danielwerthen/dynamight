using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public abstract class TextureProgram : Program
    {
        public abstract void Resize(Size size);
        public abstract QuickDraw Draw();
        public abstract void LoadBitmap(Bitmap bitmap);
    }
}
