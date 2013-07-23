using Graphics.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class Renderable
    {
        public Renderable()
        {
        }

        public Shape Shape { get; set; }
        public Animatable Animatable { get; set; }
        
        private bool _visible = true;
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

    }
}
