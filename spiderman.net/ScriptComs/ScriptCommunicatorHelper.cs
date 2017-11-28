using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

/// <summary>
/// Written by stillhere (LfxB)
/// Source: https://github.com/LfxB/ScriptCommunicatorMenu
/// </summary>
namespace ScriptCommunicatorHelper
{
    class ScriptCommunicator
    {
        EventWaitHandle MainHandle { get; set; }
        EventWaitHandle ScriptCommunicatorModMenuHandle;
        bool ScriptCommunicatorModMenuIsBlocked;
        string EventName;

        public ScriptCommunicator(string EventName)
        {
            this.EventName = EventName;
            MainHandle = new EventWaitHandle(false, EventResetMode.ManualReset, EventName);
            ScriptCommunicatorModMenuHandle = new EventWaitHandle(true, EventResetMode.ManualReset, "ScriptCommunicator");
        }

        public void Init(string title, string description)
        {
            string path = ".\\scripts\\" + EventName + ".scmod";
            string text = title + Environment.NewLine + description;
            if (File.Exists(path)) text = File.ReadAllText(path);
            File.WriteAllText(path, title + Environment.NewLine + description);
        }

        public bool IsEventTriggered()
        {
            return MainHandle.WaitOne(0);
        }

        public void TriggerEvent()
        {
            MainHandle.Set();
        }

        public void ResetEvent()
        {
            MainHandle.Reset();
        }

        public void BlockScriptCommunicatorModMenu()
        {
            if (!ScriptCommunicatorModMenuIsBlocked)
            {
                bool waitHandleExists = EventWaitHandle.TryOpenExisting("ScriptCommunicator", out ScriptCommunicatorModMenuHandle);
                if (waitHandleExists) { ScriptCommunicatorModMenuHandle.Reset(); ScriptCommunicatorModMenuIsBlocked = true; }
            }
            else
            {
                ScriptCommunicatorModMenuHandle.Reset();
            }
        }

        public void UnblockScriptCommunicatorModMenu()
        {
            if (ScriptCommunicatorMenuIsBlocked() && ScriptCommunicatorModMenuIsBlocked)
            {
                ScriptCommunicatorModMenuHandle.Set();
                ScriptCommunicatorModMenuIsBlocked = false;
            }
        }

        public bool ScriptCommunicatorMenuIsBlocked()
        {
            return !ScriptCommunicatorModMenuHandle.WaitOne(0);
        }

        public bool ScriptCommunicatorMenuDllExists()
        {
            return File.Exists(@"scripts\ScriptCommunicator.dll");
        }
    }
}