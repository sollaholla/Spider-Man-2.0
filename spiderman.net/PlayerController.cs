using System;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Native;
using SpiderMan.Abilities.SpecialAbilities;
using SpiderMan.Abilities.SpecialAbilities.PlayerOnly;
using SpiderMan.Library.Memory;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Modding.Stillhere;
using SpiderMan.ProfileSystem.SpiderManScript;

/// <summary>
/// Credits: 
/// - stillhere: https://www.gta5-mods.com/users/stillhere
/// - crosire: https://www.gta5-mods.com/users/crosire
/// - alexander blade: http://www.dev-c.com/
/// </summary>
namespace SpiderMan
{
    /// <summary>
    ///     The main script that controls all our abilities.
    /// </summary>
    public class PlayerController : Script
    {
        /// <summary>
        ///     This holds a list of our special abilities.
        /// </summary>
        private List<SpecialAbility> _abilities = new List<SpecialAbility>();

        /// <summary>
        ///     Used for initialization the first time our tick method is called.
        /// </summary>
        private bool _initAbilities;

        /// <summary>
        ///     Called the first tick.
        /// </summary>
        private bool _initMemory;

        /// <summary>
        /// The main menu.
        /// </summary>
        private readonly UIMenu _mainMenu;

        /// <summary>
        /// True if we blocked the script communicator menu.
        /// </summary>
        private bool _wasScriptCommsBlocked;

        /// <summary>
        /// The menu pool.
        /// </summary>
        private readonly MenuPool _menuPool;

        /// <summary>
        /// The profiles found in the files directory.
        /// </summary>
        private readonly List<SpiderManProfile> _profiles = new List<SpiderManProfile>();

        /// <summary>
        /// The profile that controls the player.
        /// </summary>
        private static SpiderManProfile _spiderManProfile;

        /// <summary>
        ///     The script communicator. (used by the script communicator menu)
        /// </summary>
        private readonly ScriptCommunicator _scriptComms = new ScriptCommunicator("SpiderMan");

        /// <summary>
        /// Set to true for the profile to be reset.
        /// </summary>
        private bool _setProfileNull = false;

        /// <summary>
        ///     The main constructor.
        /// </summary>
        public PlayerController()
        {
            Tick += OnTick;
            Aborted += OnAborted;
            Interval = 0;

            _scriptComms.Init("Spider-Man Script", "Version 2.0");
            _mainMenu = new UIMenu("Spider-Man Script");
            _menuPool = new MenuPool();
            _menuPool.AddMenu(_mainMenu);

            var files = Directory.GetFiles(".\\scripts\\Spider-Man Files\\Profiles\\", "*.ini");
            foreach (var file in files)
            {
                var profile = new SpiderManProfile(file);
                profile.Init();
                profile.AddToMenu(_mainMenu, _menuPool);
            }

            SpiderManProfile.ProfileActivated += (sender, args, profile) =>
            {
                _spiderManProfile = (SpiderManProfile)profile;
            };

            var deactivateButton = new UIMenuItem("Deactivate Powers");
            _mainMenu.AddMenuItem(deactivateButton);
            _mainMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == deactivateButton && _spiderManProfile != null)
                    _setProfileNull = true;
            };
        }

        /// <summary>
        ///     The local player's character component.
        /// </summary>
        public static Ped PlayerCharacter { get; private set; }

        /// <summary>
        /// True if this mod is enabled.
        /// </summary>
        public static bool ModEnabled => Entity.Exists(_spiderManProfile?.LocalUser);

        /// <summary>
        ///     Called when the mod is reloaded / crashed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAborted(object sender, EventArgs e)
        {
            StopAllAbilities();
        }

        /// <summary>
        ///     Calls the stop method on all the player's abilities.
        /// </summary>
        private void StopAllAbilities()
        {
            // Stop all of our abilities.
            foreach (var ability in _abilities)
                ability.Stop();
        }

        /// <summary>
        ///     Our update method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTick(object sender, EventArgs e)
        {
            InitMemory();
            UpdateMenus();

            if (!ModEnabled || _setProfileNull)
            {
                if (!_initAbilities) return;
                StopAllAbilities();
                _initAbilities = false;
                _spiderManProfile = null;
                _setProfileNull = false;
                return;
            }

            // Update our player's character in case he changes models.
            if (!Entity.Exists(PlayerCharacter))
            {
                // Set the new ped.
                SetPlayerCharacter();
                StopAllAbilities();
                _initAbilities = false;
            }

            // Initialize our abilities.
            InitAbilities();
            UpdateAbilities();

            Function.Call((Hash)0xC388A0F065F5BC34, Game.Player.Handle, 1f);
            Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, Game.Player.Handle,
                _spiderManProfile.HealthRechargeMultiplier);

            // Stop the player from using/having a parachute.
            StopPlayerFromUsingParachute();
        }

        private void UpdateMenus()
        {
            // Process the menus.
            _menuPool.ProcessMenus();

            if (_scriptComms.IsEventTriggered())
            {
                Wait(100); // Wait so input doesn't trigger again.
                _mainMenu.IsVisible = true;
                _scriptComms.ResetEvent();
                _scriptComms.BlockScriptCommunicatorModMenu();
                _wasScriptCommsBlocked = true;
            }

            if (_mainMenu.IsVisible) return;
            if (!_wasScriptCommsBlocked) return;
            _wasScriptCommsBlocked = false;
            _scriptComms.UnblockScriptCommunicatorModMenu();
        }

        private void UpdateAbilities()
        {
            // Here's where we're going to set the player's super jump.
            Game.Player.SetSuperJumpThisFrame();

            // If the gameplay camera is not rendering (menyoo spooner for example),
            // then just return until otherwise.
            if (!GameplayCamera.IsRendering)
                return;

            // Now we need to update each of these abilities.
            foreach (var ability in _abilities)
                ability.Update();
        }

        private static void SetPlayerCharacter()
        {
            PlayerCharacter = Game.Player.Character;
            PlayerCharacter.MaxHealth = _spiderManProfile.MaxHealth;
            PlayerCharacter.Health = PlayerCharacter.MaxHealth;
        }

        private static void StopPlayerFromUsingParachute()
        {
            // Remove the player parachute.
            Function.Call(Hash.SET_PLAYER_HAS_RESERVE_PARACHUTE, Game.Player.Handle, false);
            if (PlayerCharacter.Weapons.HasWeapon(WeaponHash.Parachute))
                PlayerCharacter.Weapons.Remove(WeaponHash.Parachute);
        }

        private void InitAbilities()
        {
            if (_initAbilities) return;
            _abilities = new List<SpecialAbility>
            {
                new Agility(_spiderManProfile),
                new Melee(_spiderManProfile),
                new StarkTech(_spiderManProfile),
                new WallCrawl(_spiderManProfile)
            };
            _initAbilities = true;
        }

        private void InitMemory()
        {
            if (_initMemory) return;
            HandlingFile.Init();
            //CTimeScale.Init();
            _initMemory = true;
        }
    }
}