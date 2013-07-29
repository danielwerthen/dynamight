using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public class TextureProgram : Program
    {

        Bitmap bitmap;
        int texture;
        protected TextureUnit unit = TextureUnit.Texture0;
        protected ProgramWindow parent;


        public virtual void Resize(Size size)
        {
            this.bitmap = new Bitmap(this.bitmap, size);
            LoadBitmap(this.bitmap);
        }

        public virtual QuickDraw Draw()
        {
            return QuickDraw.Start(this.bitmap, () => LoadBitmap(this.bitmap));
        }

        public virtual void LoadBitmap(Bitmap bitmap)
        {
            if (parent == null)
                throw new Exception("Can not load bitmap since the program hasn't been activated yet.");
            parent.UpdateTexture(bitmap, texture);
            this.bitmap = bitmap;
        }

        public override void Load(ProgramWindow parent)
        {
            this.parent = parent;
            this.bitmap = this.bitmap ?? new System.Drawing.Bitmap(parent.Width, parent.Height);
            texture = parent.LoadTexture(this.bitmap, unit);
        }

        public override void Unload()
        {
            if (texture != 0)
                GL.DeleteTexture(texture);
        }

        public override void Render()
        {
        }
    }
}
