
// Shader entry point, this is executed per Pixel
void main(void)
{

    gl_FragColor = vec4 (0.0,1,0,0.5);
  //gl_FragColor = texture1D( COLORTABLE, FinalTableIndex ); // lookup texture for output
// gl_FragColor.rgb = vec3(FinalTableIndex); // Debug: output greyscale
}