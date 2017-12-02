using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using SpiderMan.Abilities.SpecialAbilities;
using SpiderMan.Library.Memory;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Modding.Stillhere;

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
    public class CoreScript : Script
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
        ///     The script communicator. (used by the script communicator menu)
        /// </summary>
        private readonly ScriptCommunicator _scriptComms = new ScriptCommunicator("SpiderMan");

        /// <summary>
        ///     The main constructor.
        /// </summary>
        public CoreScript()
        {
            Tick += OnTick;
            Aborted += OnAborted;
            Interval = 0;

            _scriptComms.Init("Spider-Man Script", "Version 2.0");
            _mainMenu = new UIMenu("Spider-Man Script");
            _menuPool = new MenuPool();
            _menuPool.AddMenu(_mainMenu);

            var enableItem = new UIMenuItem("Activate Powers") { Tag = "m_Enabled", Description = "Enable the player's powers." };
            var disableItem = new UIMenuItem("Disable Powers") { Tag = "m_Disabled", Description = "Disable the player's powers." };
            _mainMenu.AddMenuItem(enableItem);
            _mainMenu.AddMenuItem(disableItem);

            _mainMenu.OnItemSelect += (a0, item, a2) =>
            {
                if (item == enableItem)
                    ModEnabled = true;
                else if (item == disableItem)
                    ModEnabled = false;
            };
        }

        /// <summary>
        ///     The local player's character component.
        /// </summary>
        public static Ped PlayerCharacter { get; private set; }

        /// <summary>
        /// True if this mod is enabled.
        /// </summary>
        public static bool ModEnabled { get; private set; }

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
            UpdateMenus();

            if (!ModEnabled)
            {
                if (!_initAbilities) return;
                StopAllAbilities();
                _initAbilities = false;
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

            Function.Call((Hash)0xC388A0F065F5BC34, Game.Player.Handle, 1f);
            Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, Game.Player.Handle, 1000f);

            // Stop the player from using/having a parachute.
            StopPlayerFromUsingParachute();

            // Initialize our abilities.
            Init();
            UpdateAbilities();
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
            PlayerCharacter.MaxHealth = 3000;
            PlayerCharacter.Health = 3000;
        }

        private static void StopPlayerFromUsingParachute()
        {
            // Remove the player parachute.
            Function.Call(Hash.SET_PLAYER_HAS_RESERVE_PARACHUTE, Game.Player.Handle, false);
            if (PlayerCharacter.Weapons.HasWeapon(WeaponHash.Parachute))
                PlayerCharacter.Weapons.Remove(WeaponHash.Parachute);
        }

        /// <summary>
        ///     Here's where we initialize our abilities and memory stuff.
        /// </summary>
        private void Init()
        {
            InitMemory();
            InitAbilities();
        }

        private void InitAbilities()
        {
            if (_initAbilities) return;
            _abilities = new List<SpecialAbility>
            {
                new Agility(),
                new Melee(),
                new StarkTech(),
                new WallCrawl()
            };
            _initAbilities = true;
        }

        private void InitMemory()
        {
            if (_initMemory) return;
            HandlingFile.Init();
            CTimeScale.Init();
            _initMemory = true;
        }
    }
}