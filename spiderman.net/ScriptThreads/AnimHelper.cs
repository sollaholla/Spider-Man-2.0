using GTA;
using GTA.Native;
using SpiderMan.Library.Extensions;

namespace SpiderMan.ScriptThreads
{
    public class AnimHelper : Script
    {
        public AnimHelper()
        {
            Tick += OnTick;
        }

        public Ped PlayerCharacter => CoreScript.PlayerCharacter;

        private void OnTick(object sender, System.EventArgs e)
        {
            if (!CoreScript.ModEnabled)
                return;

            if (PlayerCharacter.IsGettingUp)
            {
                if (PlayerCharacter.IsPlayingAnimation("swimming@swim", "recover_back_to_idle"))
                    PlayerCharacter.Task.ClearAnimation("swimming@swim", "recover_back_to_idle");

                if (PlayerCharacter.IsPlayingAnimation("swimming@swim", "recover_flip_back_to_front"))
                    PlayerCharacter.Task.ClearAnimation("swimming@swim", "recover_flip_back_to_front");

                if (PlayerCharacter.IsPlayingAnimation("weapons@projectile@", "throw_m_fb_stand"))
                    PlayerCharacter.Task.ClearAnimation("weapons@projectile@", "throw_m_fb_stand");

                //if (PlayerCharacter.IsPlayingAnimation("move_fall", "land_roll"))
                //    PlayerCharacter.Task.ClearAnimation("move_fall", "land_roll");
            }

            Function.Call(Hash.SET_PED_CAN_ARM_IK, PlayerCharacter,
                !PlayerCharacter.IsPlayingAnimation("move_fall", "land_roll") && !PlayerCharacter.IsGettingUp);

            //if (!PlayerCharacter.GetConfigFlag(60))
            //{
            //    if (PlayerCharacter.IsPlayingAnimation("move_fall", "land_roll"))
            //        PlayerCharacter.Task.ClearAnimation("move_fall", "land_roll");
            //}
        }
    }
}
