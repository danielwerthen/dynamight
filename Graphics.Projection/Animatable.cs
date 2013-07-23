using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public abstract class Animatable
    {
        public abstract Matrix4 GetModelView(double time);
    }

    public class Translator : Animatable
    {

        protected Matrix4 translation;
        public void SetPosition(Vector3 pos)
        {
            translation = Matrix4.CreateTranslation(pos);
        }

        public override Matrix4 GetModelView(double time)
        {
            return translation;
        }
    }

    public class RadialSpin : Translator
    {
        Vector3 center;
        public RadialSpin(Vector3 center)
        {
            this.center = center;
        }

        public override Matrix4 GetModelView(double time)
        {
            var rotation = Matrix4.CreateRotationZ((float)(time * 0.5 * Math.PI));
            var toCenter = Matrix4.CreateTranslation(center);
            var fromCenter = Matrix4.CreateTranslation(center);
            fromCenter.Invert();
            return toCenter * rotation * fromCenter * translation;
        }
    }
}
