using Graphics.Input;
using Graphics.Projection.Lights;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class MoveableLights : IEnumerable<LightSourceParameters>
    {
        LightSourceParameters[] lights;
        public MoveableLights(int count)
        {
            lights = (Dynamight.ImageProcessing.CameraCalibration.Range.OfInts(count)).Select(_ => new LightSourceParameters()).ToArray();
        }

        public LightSourceParameters this[int i]
        {
            get { return lights[i]; }
            set { lights[i] = value; }
        }

        public int Activate()
        {
            int c = 0;
            var lids = lights.Where(l => l.InUse).Select(_ => c++).ToArray();
            lids.Zip(lights.Where(l => l.InUse), (i, l) => l.Set(i)).ToArray();
            return c;
        }

        public void UseInput(KeyboardDevice keyboard)
        {
            var keyl = new KeyboardListener(keyboard);

            keyl.AddAction(() => Selection = (Selection == 0 ? null : (int?)0), Key.F1);
            keyl.AddAction(() => Selection = (Selection == 1 ? null : (int?)1), Key.F2);
            keyl.AddAction(() => Selection = (Selection == 2 ? null : (int?)2), Key.F3);
            keyl.AddAction(() => Selection = (Selection == 3 ? null : (int?)3), Key.F4);
            keyl.AddAction(() => Selection = (Selection == 4 ? null : (int?)4), Key.F5);
            keyl.AddAction(() => Selection = (Selection == 5 ? null : (int?)5), Key.F6);
            keyl.AddAction(() => Selection = (Selection == 6 ? null : (int?)6), Key.F7);
            keyl.AddAction(() => Selection = (Selection == 7 ? null : (int?)7), Key.F8);

            keyl.AddAction(ActivateLight, Key.A);

            keyl.AddBinaryAction(0.01f, -0.01f, Key.Right, Key.Left, null, (f) => MoveX(f));
            keyl.AddBinaryAction(0.01f, -0.01f, Key.Down, Key.Up, null, (f) => MoveY(f));
            keyl.AddBinaryAction(0.01f, -0.01f, Key.Down, Key.Up, new Key[] { Key.ShiftLeft }, (f) => MoveZ(f));

            keyl.AddBinaryAction(0.05f, -0.05f, Key.Right, Key.Left, new Key[] { Key.ControlLeft }, (f) => MoveX(f));
            keyl.AddBinaryAction(0.05f, -0.05f, Key.Down, Key.Up, new Key[] { Key.ControlLeft }, (f) => MoveY(f));
            keyl.AddBinaryAction(0.05f, -0.05f, Key.Down, Key.Up, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (f) => MoveZ(f));
        }

        int? Selection = null;

        private void ActivateLight()
        {
            if (Selection == null)
                return;
            else
            {
                var l = lights[Selection.Value];
                l.InUse = !l.InUse;
            }
        }

        private void MoveX(float f)
        {
            if (Selection != null)
            {
                var l = lights[Selection.Value];
                l.Position.X += f;
                Console.Clear();
                Console.WriteLine(l.Position.ToString());
            }
        }

        private void MoveY(float f)
        {
            if (Selection != null)
            {
                var l = lights[Selection.Value];
                l.Position.Y += f;
                Console.Clear();
                Console.WriteLine(l.Position.ToString());
            }
        }

        private void MoveZ(float f)
        {
            if (Selection != null)
            {
                var l = lights[Selection.Value];
                l.Position.Z += f;
                Console.Clear();
                Console.WriteLine(l.Position.ToString());
            }
        }

        public IEnumerator<LightSourceParameters> GetEnumerator()
        {
            return lights.AsEnumerable<LightSourceParameters>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return lights.GetEnumerator();
        }
    }
}
