using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public static class GLSLSnippets
    {
        public const string VSDistort =
@"
uniform int WIDTH;
uniform int HEIGHT;
uniform vec2 F;
uniform vec2 C;
uniform mat4 K;

float rad(float x, float y)
{
    return pow(x,2.0) + pow(y,2.0);
}

float dist(float r2)
{
    return (1.0 + K[0][0] * r2 + K[1][0] * pow(r2, 2.0) + K[0][1] * pow(r2, 3.0))
        /  (1.0 + K[1][1] * r2 + K[2][1] * pow(r2, 2.0) + K[3][1] * pow(r2, 3.0));
}

vec4 distort(vec4 V)
{
  float xp = V.x;
  float yp = V.y;
  float z = V.z;

  if (V.z == 0.)
  {
    xp = xp / 0.0001;
    yp = yp / 0.0001;
    z = 0.;
  } else
  {
    xp = xp / V.z;
    yp = yp / V.z;
    z = 0.5;
  }
  float r2 = rad(xp, yp);
  float xpp = xp * dist(r2) + 2.0 * K[2][0] * xp * yp + K[3][0] * (r2 + 2.0 * pow(xp, 2.0));
  float ypp = yp * dist(r2) + K[2][0] * (r2 + 2.0 * pow(yp, 2.0)) + 2.0 * K[3][0] * xp * yp;
  float u = F.x * xpp + C.x;
  float v = F.y * ypp + C.y;

  return vec4(2.0 * (u / float(WIDTH)) - 1.0, 2.0 * ((float(HEIGHT) - v) / float(HEIGHT)) - 1.0, 0.5, 1.0);
}
";

        public const string VSLighting = @"
varying vec3 N;
varying vec3 v;
void main(void)
{
    gl_FrontColor = gl_Color;
    gl_TexCoord[0] = gl_MultiTexCoord0; 
    vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
    v = vec3(gl_ModelViewProjectionMatrix * gl_Vertex);       
    N = normalize(gl_NormalMatrix * gl_Normal);
    gl_Position = distort(V);

}
";
        public const string VSLightingUndistorted = @"
varying vec3 N;
varying vec3 v;
void main(void)
{
    gl_FrontColor = gl_Color;
    gl_TexCoord[0] = gl_MultiTexCoord0; 
    vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
    v = vec3(gl_ModelViewMatrix * gl_Vertex);       
    N = normalize(gl_NormalMatrix * gl_Normal);
    gl_Position = V;

}
";

        public const string FSLightFun = @"
vec4 getLightComp(gl_LightSourceParameters light, vec3 v, vec3 N, vec4 mat)
{
    vec3 D = v - light.position.xyz;
    float dist = distance(light.position.xyz, v);
    float attn = 1.0 / (light.constantAttenuation + 
                        light.linearAttenuation * dist + 
                        light.quadraticAttenuation * dist * dist);

    vec3 L = normalize(D);
    vec4 Idiff = attn * light.diffuse * max(dot(N,L), 0.0) + light.ambient;
    Idiff = clamp(Idiff * mat, 0.0, 1.0);
    return Idiff;
}
";

        public const string FSMultiLight = @"
varying vec3 N;
varying vec3 v;
uniform int MAXLIGHTS;

void main(void)
{
    vec4 col = vec4(0,0,0,1);
    for (int i = 0; i < MAXLIGHTS; i++)
    {
        col = col + getLightComp(gl_LightSource[i], v, N, gl_Color);
    }
    gl_FragColor = clamp(col, 0.0, 1.0);
}
";

        public const string FSLighting = @"

varying vec3 N;
varying vec3 v;
uniform sampler2D COLORTABLE;

void main(void)
{
   vec3 D = v - gl_LightSource[0].position.xyz;
   D = vec3(0.0,0.0,1.0);
   float dist = distance(gl_LightSource[0].position.xyz, v);
   float attn = 1.0/( gl_LightSource[0].constantAttenuation + 
                    gl_LightSource[0].linearAttenuation * dist +
                    gl_LightSource[0].quadraticAttenuation * dist * dist );
   attn = 0.99;
   vec3 L = normalize(D);   
   vec4 Idiff = attn * gl_FrontLightProduct[0].diffuse * max(dot(N,L), 0.0) + gl_FrontLightProduct[0].ambient;  
   Idiff = clamp(Idiff, 0.0, 1.0); 

   gl_FragColor = Idiff * gl_Color;
}
";

        public const string FSStatic = @"

varying vec3 N;
varying vec3 v;
uniform sampler2D COLORTABLE;

void main(void)
{
   vec3 D = vec3(0.0,0.0,1.0);
   float attn = 0.99;
   vec3 L = normalize(D);   
   vec4 Idiff = attn * vec4(1.,1.,1.,1.) * max(dot(N,L), 0.0) + vec4(0.1,0.1,0.1,0.1);  
   Idiff = clamp(Idiff, 0.0, 1.0); 

   gl_FragColor = Idiff * gl_Color;
}
";
    }

    public class GLSLPointCloudProgram : GLSLProgram
    {
        

        public GLSLPointCloudProgram(bool projection)
        {
            this.VS = projection ? GLSLSnippets.VSDistort + GLSLSnippets.VSLighting : GLSLSnippets.VSLightingUndistorted;
            this.FS = GLSLSnippets.FSLightFun + GLSLSnippets.FSMultiLight;
        }

        public void Setup(int maxLights)
        {
            GL.Uniform1(GL.GetUniformLocation(program, "MAXLIGHTS"), maxLights);
        }
    }

    public class GLSLProgram
    {
        public string VS;
        public string FS;
        protected int program;
        public void Load()
        {
            var vs = GraphicsWindow.CreateShader2(ShaderType.VertexShader, VS);
            var fs = GraphicsWindow.CreateShader2(ShaderType.FragmentShader, FS);
            program = GraphicsWindow.CreateProgram2(vs, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
        }

        public void Unload()
        {
            if (program != 0)
                GL.DeleteProgram(program);
        }

        public void Activate()
        {
            GL.UseProgram(program);
        }
    }
}
