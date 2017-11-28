﻿using GTA;
using GTA.Native;
using ScriptCommunicatorHelper;
using spiderman.net.Abilities;
using spiderman.net.Library;
using spiderman.net.Library.Extensions;
using spiderman.net.Library.Memory;
using System.Collections.Generic;

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

            _scriptComms.Init("Spider-Man V", "by Sollaholla");
        }

        /// <summary>
        /// The local player's character component.
        /// </summary>
        public static Ped PlayerCharacter { get; private set; }

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
                PlayerCharacter.MaxHealth = 5000;
                PlayerCharacter.Health = 5000;
                StopAllAbilities();
                _initialized = false;
            }

            // Make sure the player doesn't have parachutes..
            Function.Call(Hash.SET_PLAYER_HAS_RESERVE_PARACHUTE, Game.Player.Handle, false);

            // Remove the player parachute.
            if (PlayerCharacter.Weapons.HasWeapon(WeaponHash.Parachute))
                PlayerCharacter.Weapons.Remove(WeaponHash.Parachute);

            // Initialize our abilities.
            Init();

            if (!GameplayCamera.IsRendering)
                return;

            // Now we need to update each of these abilities.
            foreach (var ability in _abilities)
                ability.Update();
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