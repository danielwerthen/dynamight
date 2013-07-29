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

    public class StaticAnima : Animatable
    {
        Matrix4 transform;
        public StaticAnima(Matrix4 mx)
        {
            transform = mx;
        }

        public override Matrix4 GetModelView(double time)
        {
            return transform;
        }
    }

    public class Combiner : Animatable
    {
        Animatable[] animators;
        public Combiner(params Animatable[] animators)
        {
            this.animators = animators;
        }

        public override Matrix4 GetModelView(double time)
        {
            return animators.Select(a => a.GetModelView(time)).Aggregate((m1, m2) => m1 * m2);
        }
    }

    public class Translator : Animatable
    {
        public Translator()
        {
            translation = Matrix4.Identity;
        }

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
