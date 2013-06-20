
uniform int WIDTH;
uniform int HEIGHT;
uniform int STEP;
uniform int LENGTH;
uniform int ROWS;
uniform vec4 COLOR;

// Shader entry point, this is executed per Pixel
void main(void)
{
if (ROWS == 0)
{
  if (gl_FragCoord.x > float(STEP * LENGTH) && gl_FragCoord.x < float((STEP + 1) * LENGTH))
  {
    gl_FragColor = COLOR;
  }
  else
  {
    gl_FragColor = vec4 (0,0,0,0);
  }
}
else
{
  if (gl_FragCoord.y > float(STEP * LENGTH) && gl_FragCoord.y < float((STEP + 1) * LENGTH))
  {
    gl_FragColor = COLOR;
  }
  else
  {
    gl_FragColor = vec4 (0,0,0,0);
  }
}
  //gl_FragColor = texture1D( COLORTABLE, FinalTableIndex ); // lookup texture for output
// gl_FragColor.rgb = vec3(FinalTableIndex); // Debug: output greyscale
}