using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Input
{

    public class KeyboardAction
    {
        public Key[] Modifiers;
        public Key Key;
        public Action Action;
        public KeyboardAction()
        {

        }
    }
}
