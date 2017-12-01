using GTA;
using GTA.Native;
using ScriptCommunicatorHelper;
using SimpleUI;
using spiderman.net.Abilities;
using spiderman.net.Abilities.Types;
using spiderman.net.Library;
using spiderman.net.Library.Memory;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Credits: 
/// - stillhere: https://www.gta5-mods.com/users/stillhere
/// - crosire: https://www.gta5-mods.com/users/crosire
/// - alexander blade: http://www.dev-c.com/
/// </summary>
namespace spiderman.net
{
    /// <summary>
    /// The main script that controls all our abilities.
    /// </summary>
    public class CoreScript : Script
    {
        /// <summary>
        /// The script communicator. (used by the script communicator menu)
        /// </summary>
        private ScriptCommunicator _scriptComms = new ScriptCommunicator("SpiderMan");

        /// <summary>
        /// This holds a list of our special abilities.
        /// </summary>
        private List<SpecialAbility> _abilities = new List<SpecialAbility>();

        /// <summary>
        /// The main menu.
        /// </summary>
        private readonly UIMenu _mainMenu = new UIMenu("Spider-Man V");

        /// <summary>
        /// The menu processor.
        /// </summary>
        private readonly MenuPool _menuPool = new MenuPool();

        /// <summary>
        /// Used for initialization the first time our tick method is called.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// Called the first tick.
        /// </summary>
        private bool _initMemory;

        /// <summary>
        /// The main constructor.
        /// </summary>
        public CoreScript()
        {

            Tick += OnTick;
            Aborted += OnAborted;
            Interval = 0;

            PlayerProfile = new ProfileSettings();
            if (!File.Exists(PlayerProfile.Path))
                PlayerProfile.Write();
            else PlayerProfile.Read();

            _scriptComms.Init("Spider-Man V", "by Sollaholla");
        }

        /// <summary>
        /// The local player's character component.
        /// </summary>
        public static Ped PlayerCharacter { get; private set; }

        /// <summary>
        /// The player's profile settings.
        /// </summary>
        public static ProfileSettings PlayerProfile { get; private set; }

        /// <summary>
        /// True if the mod is ready to be used.
        /// </summary>
        public static bool ModEnabled { get; private set; }

        /// <summary>
        /// Called when the mod is reloaded / crashed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAborted(object sender, System.EventArgs e)
        {
            StopAllAbilities();
        }

        /// <summary>
        /// Calls the stop method on all the player's abilities.
        /// </summary>
        private void StopAllAbilities()
        {
            // Stop all of our abilities.
            foreach (var ability in _abilities)
                ability.Stop();
        }

        /// <summary>
        /// Our update method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTick(object sender, System.EventArgs e)
        {
            // Update our player's character in case he changes models.
            if (!Entity.Exists(PlayerCharacter))
            {
                // Set the new ped.
                PlayerCharacter = Game.Player.Character;
                PlayerCharacter.MaxHealth = PlayerProfile.MaxHealth;
                PlayerCharacter.Health = PlayerProfile.MaxHealth;
                StopAllAbilities();
                ConfigurePlayer();
                _initialized = false;
            }

            // Update the menus.
            UpdateMenus();

            // Initialize our abilities.
            Init();

            if (!GameplayCamera.IsRendering)
                return;

            // Now we need to update each of these abilities.
            foreach (var ability in _abilities)
                ability.Update();
        }

        private void UpdateMenus()
        {
            _menuPool.ProcessMenus();
            if (_scriptComms.IsEventTriggered())
            {
                _mainMenu.IsVisible = !_mainMenu.IsVisible;
            }
        }

        /// <summary>
        /// Configures some default values for the player.
        /// </summary>
        private static void ConfigurePlayer()
        {
            Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, Game.Player.Handle, PlayerProfile.HealthRechargeMultiplier);
            Function.Call(Hash.SET_PLAYER_HAS_RESERVE_PARACHUTE, Game.Player.Handle, false);
            Function.Call((Hash)0xC388A0F065F5BC34, Game.Player.Handle, 1f);
            if (PlayerCharacter.Weapons.HasWeapon(WeaponHash.Parachute))
                PlayerCharacter.Weapons.Remove(WeaponHash.Parachute);
        }

        /// <summary>
        /// Here's where we initialize our abilities.
        /// </summary>
        private void Init()
        {
            if (!_initMemory)
            {
                HandlingFile.Init();
                CTimeScale.Init();
                _initMemory = true;
            }

            if (!_initialized)
            {
                _abilities = new List<SpecialAbility>
                {
                    new Agility(),
                    new Melee(),
                    new StarkTech(),
                    new WallCrawl(),
                };
                _initialized = true;
            }
        }
    }
}
