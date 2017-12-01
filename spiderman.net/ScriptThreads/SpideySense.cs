using System;
using GTA;

namespace SpiderMan.ScriptThreads
{
    public class SpideySense : Script
    {
        private bool _spideySenseOn;

        public SpideySense()
        {
            Tick += OnTick;
            Aborted += OnAborted;
        }

        private void OnAborted(object sender, EventArgs e)
        {
            Game.TimeScale = 1.0f;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Game.DisableControlThisFrame(2, Control.SpecialAbility);
            Game.DisableControlThisFrame(2, Control.SpecialAbilityPC);
            Game.DisableControlThisFrame(2, Control.SpecialAbilitySecondary);

            if (Game.IsDisabledControlJustPressed(2, Control.SpecialAbility) ||
                Game.IsDisabledControlJustPressed(2, Control.SpecialAbilityPC))
                _spideySenseOn = !_spideySenseOn;

            if (_spideySenseOn)
                Game.TimeScale = 0.1f;
            else Game.TimeScale = 1f;
        }
    }
}