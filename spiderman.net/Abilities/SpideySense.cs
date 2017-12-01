using GTA;
using GTA.Math;
using spiderman.net.Library;
using spiderman.net.Library.Extensions;
using System.Drawing;

namespace spiderman.net.Abilities
{
    public class SpideySense : Script
    {
        private bool _spideySenseOn = false;

        public SpideySense()
        {
            Tick += OnTick;
            Aborted += OnAborted;
        }

        private void OnAborted(object sender, System.EventArgs e)
        {
            Game.TimeScale = 1.0f;
        }

        private void OnTick(object sender, System.EventArgs e)
        {
            if (!CoreScript.ModEnabled)
            {
                if (_spideySenseOn)
                {
                    Game.TimeScale = 1.0f;
                    _spideySenseOn = false;
                }
                return;
            }

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
