using GTA;
using System;

namespace spiderman.net.Scripts
{
    public class Controls : Script
    {
        private static Control[] controls;

        static Controls()
        {
            controls = (Control[])Enum.GetValues(typeof(Control));
        }

        public Controls()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!CoreScript.ModEnabled)
                return;

            Game.DisableControlThisFrame(2, Control.SelectWeapon);
            Game.DisableControlThisFrame(2, Control.Cover);
            Game.DisableControlThisFrame(2, Control.ParachuteDeploy);
            Game.DisableControlThisFrame(2, Control.MeleeAttack1);
            Game.DisableControlThisFrame(2, Control.MeleeAttack2);
            Game.DisableControlThisFrame(2, Control.MeleeAttackAlternate);
            Game.DisableControlThisFrame(2, Control.MeleeAttackHeavy);
            Game.DisableControlThisFrame(2, Control.MeleeAttackLight);
            Game.DisableControlThisFrame(2, Control.MeleeBlock);
            Game.DisableControlThisFrame(2, Control.SelectWeaponMelee);
            Game.DisableControlThisFrame(2, Control.LookBehind);
        }

        /// <summary>
        /// Disables all controls except for record buttons.
        /// </summary>
        public static void DisableControlsKeepRecording(int group)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                var c = controls[i];
                if (c == Control.ReplayStartStopRecording ||
                    c == Control.ReplayStartStopRecordingSecondary)
                    continue;

                Game.DisableControlThisFrame(group, c);
            }
        }
    }
}
