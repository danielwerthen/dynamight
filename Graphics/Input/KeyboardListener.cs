using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Input
{
    public class KeyboardListener
    {
        private List<KeyboardAction> actions = new List<KeyboardAction>();
        private KeyboardDevice device;
        private Dictionary<Key, bool> modifiers = new Dictionary<Key, bool>();
        private readonly Key[] Modifiers = new Key[] {
            Key.ShiftLeft,
            Key.ShiftRight,
            Key.ControlLeft,
            Key.ControlRight,
            Key.AltLeft,
            Key.AltRight
        };

        public KeyboardListener(KeyboardDevice device)
        {
            this.device = device;
            this.device.KeyDown += device_KeyDown;
            this.device.KeyUp += device_KeyUp;
            this.device.KeyRepeat = true;

            foreach (var mod in Modifiers)
                modifiers[mod] = false;
        }

        void device_KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            if (Modifiers.Contains(e.Key))
                modifiers[e.Key] = false;
        }

        void device_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (Modifiers.Contains(e.Key))
                modifiers[e.Key] = true;
            foreach (var action in actions.Where(a => a.Key == e.Key).Where(a => 
                a.Modifiers == null ? !modifiers.Values.Any(v => v) : modifiers.All(m => m.Value ? a.Modifiers.Contains(m.Key) : !a.Modifiers.Contains(m.Key)) ))
                action.Action();

        }

        public void AddAction(KeyboardAction action)
        {
            actions.Add(action);
        }

        public void AddAction(Action action, Key key, Key[] mod = null)
        {
            actions.Add(new KeyboardAction() { Action = action, Key = key, Modifiers = mod });
        }

        public void AddBinaryAction<T>(T inc, T decr, Key keyInc, Key keyDecr, Key[] mod, Action<T> action)
        {
            actions.Add(new KeyboardAction() { Action = () => action(inc), Key = keyInc, Modifiers = mod });
            actions.Add(new KeyboardAction() { Action = () => action(decr), Key = keyDecr, Modifiers = mod });
        }


    }
}
