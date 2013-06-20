
uniform sampler2D COLORTABLE;
uniform int WIDTH;
uniform int HEIGHT;

// Shader entry point, this is executed per Pixel
void main(void)
{

  //gl_FragColor = texture1D( COLORTABLE, gl_FragCoord.x + gl_FragCoord.y * WIDTH );
  gl_FragColor = texture2D( COLORTABLE, vec2(gl_FragCoord.x / WIDTH,gl_FragCoord.y / HEIGHT));
  //gl_FragColor = vec4(1,0,1,1);
  //gl_FragColor = texture1D( COLORTABLE, FinalTableIndex ); // lookup texture for output
// gl_FragColor.rgb = vec3(FinalTableIndex); // Debug: output greyscale
}