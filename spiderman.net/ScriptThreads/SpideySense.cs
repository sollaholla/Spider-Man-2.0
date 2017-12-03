using System;
using System.Globalization;
using GTA;
using SpiderMan.Library.Types;

namespace SpiderMan.ScriptThreads
{
    public class SpideySense : Script
    {
        private bool _spideySenseOn;
        private float _timeScale = 0.1f;

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
            if (!PlayerController.ModEnabled)
            {
                if (!_spideySenseOn) return;
                Game.TimeScale = 0;
                _spideySenseOn = false;
                return;
            }

            Game.DisableControlThisFrame(2, Control.SpecialAbility);
            Game.DisableControlThisFrame(2, Control.SpecialAbilityPC);
            Game.DisableControlThisFrame(2, Control.SpecialAbilitySecondary);

            if (Game.IsDisabledControlJustPressed(2, Control.SpecialAbility) ||
                Game.IsDisabledControlJustPressed(2, Control.SpecialAbilityPC))
                _spideySenseOn = !_spideySenseOn;

            if (_spideySenseOn)
            {
                var scroll = Game.GetControlNormal(2, Control.CursorScrollUp);
                //UI.ShowSubtitle(scroll.ToString(CultureInfo.InvariantCulture));
                _timeScale = Maths.Clamp(_timeScale, 0.0001f, 0.2f);
            }

            Game.TimeScale = _spideySenseOn ? _timeScale : 1f;
        }
    }
}