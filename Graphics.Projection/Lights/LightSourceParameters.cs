using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection.Lights
{

    public class LightSourceParameters
    {

        public Color4 Diffuse = new Color4(255, 255, 255, 255);
        public Color4 Ambient = new Color4(55, 55, 55, 255);
        public float ConstantAttenuation = 0;
        public float LinearAttenuation = 1f;
        public float QuadraticAttenuation = 0;
        public float FieldAngle = 18;
        public float CutoffExponent = 1;
        public Vector4 SpotDirection = new Vector4(0, 0, 1, 1);
        public Vector4 Position = new Vector4(0, 0, -1, 1);
        public bool InUse = false;

        public int Set(int position)
        {
            GL.Light(LightName.Light0 + position, LightParameter.Diffuse, Diffuse);
            GL.Light(LightName.Light0 + position, LightParameter.Ambient, Ambient);
            GL.Light(LightName.Light0 + position, LightParameter.ConstantAttenuation, ConstantAttenuation);
            GL.Light(LightName.Light0 + position, LightParameter.LinearAttenuation, LinearAttenuation);
            GL.Light(LightName.Light0 + position, LightParameter.QuadraticAttenuation, QuadraticAttenuation);
            GL.Light(LightName.Light0 + position, LightParameter.Position, Position);
            GL.Light(LightName.Light0 + position, LightParameter.SpotCutoff, FieldAngle);
            GL.Light(LightName.Light0 + position, LightParameter.SpotExponent, CutoffExponent);
            GL.Light(LightName.Light0 + position, LightParameter.SpotDirection, SpotDirection);
            return position;
        }
    }
}
