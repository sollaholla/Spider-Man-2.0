using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spiderman.net.Scripts
{
    public class BackgroundThread : Script
    {
        private static List<Action> _updateMethods = new List<Action>();

        public BackgroundThread()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // Update the registered delegates but make
            // sure we're looping through a copy.
            var copy = _updateMethods.ToList();
            for (int i = 0; i < copy.Count; i++)
            {
                var m = copy[i];
                m.Invoke();
            }
        }

        /// <summary>
        /// Register a tick method to our internal list so that it runs regardless of Script.Yields();
        /// </summary>
        /// <param name="d"></param>
        public static void RegisterTick(Action d)
        {
            if (_updateMethods.Contains(d))
                return;

            _updateMethods.Add(d);
        }

        /// <summary>
        /// Unregisters this tick method from our internal list.
        /// </summary>
        /// <param name="d"></param>
        public static void UnregisterTick (Action d)
        {
            if (!_updateMethods.Contains(d))
                return;

            _updateMethods.Add(d);
        }
    }
}
