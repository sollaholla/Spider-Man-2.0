using System;
using System.Collections.Generic;
using System.Linq;
using GTA;

namespace SpiderMan.ScriptThreads
{
    public class BackgroundThread : Script
    {
        private static readonly List<Action> UpdateMethods = new List<Action>();

        public BackgroundThread()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // Update the registered delegates but make
            // sure we're looping through a copy.
            var copy = UpdateMethods.ToList();
            foreach (var m in copy)
            {
                try
                {
                    m.Invoke();
                }
                catch
                {
                    // ignored
                }
            }
        }

        /// <summary>
        ///     Register a tick method to our internal list so that it runs regardless of Script.Yields();
        /// </summary>
        /// <param name="d"></param>
        public static void RegisterTick(Action d)
        {
            if (UpdateMethods.Contains(d))
                return;

            UpdateMethods.Add(d);
        }

        /// <summary>
        ///     Unregisters this tick method from our internal list.
        /// </summary>
        /// <param name="d"></param>
        public static void UnregisterTick(Action d)
        {
            if (!UpdateMethods.Contains(d))
                return;

            UpdateMethods.Add(d);
        }
    }
}