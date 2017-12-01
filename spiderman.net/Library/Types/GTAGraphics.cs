using System.Drawing;
using GTA.Math;
using GTA.Native;

namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     A static class containing methods relating to the "GRAPHICS::" namespace in nativedb.
    /// </summary>
    public static class GTAGraphics
    {
        private static readonly string[] Eff =
        {
            "SwitchHUDIn",
            "SwitchHUDOut",
            "FocusIn",
            "FocusOut",
            "MinigameEndNeutral",
            "MinigameEndTrevor",
            "MinigameEndFranklin",
            "MinigameEndMichael",
            "MinigameTransitionOut",
            "MinigameTransitionIn",
            "SwitchShortNeutralIn",
            "SwitchShortFranklinIn",
            "SwitchShortTrevorIn",
            "SwitchShortMichaelIn",
            "SwitchOpenMichaelIn",
            "SwitchOpenFranklinIn",
            "SwitchOpenTrevorIn",
            "SwitchHUDMichaelOut",
            "SwitchHUDFranklinOut",
            "SwitchHUDTrevorOut",
            "SwitchShortFranklinMid",
            "SwitchShortMichaelMid",
            "SwitchShortTrevorMid",
            "DeathFailOut",
            "CamPushInNeutral",
            "CamPushInFranklin",
            "CamPushInMichael",
            "CamPushInTrevor",
            "SwitchSceneFranklin",
            "SwitchSceneTrevor",
            "SwitchSceneMichael",
            "SwitchSceneNeutral",
            "MP_Celeb_Win",
            "MP_Celeb_Win_Out",
            "MP_Celeb_Lose",
            "MP_Celeb_Lose_Out",
            "DeathFailNeutralIn",
            "DeathFailMPDark",
            "DeathFailMPIn",
            "MP_Celeb_Preload_Fade",
            "PeyoteEndOut",
            "PeyoteEndIn",
            "PeyoteIn",
            "PeyoteOut",
            "MP_race_crash",
            "SuccessFranklin",
            "SuccessTrevor",
            "SuccessMichael",
            "DrugsMichaelAliensFightIn",
            "DrugsMichaelAliensFight",
            "DrugsMichaelAliensFightOut",
            "DrugsTrevorClownsFightIn",
            "DrugsTrevorClownsFight",
            "DrugsTrevorClownsFightOut",
            "HeistCelebPass",
            "HeistCelebPassBW",
            "HeistCelebEnd",
            "HeistCelebToast",
            "MenuMGHeistIn",
            "MenuMGTournamentIn",
            "MenuMGSelectionIn",
            "ChopVision",
            "DMT_flight_intro",
            "DMT_flight",
            "DrugsDrivingIn",
            "DrugsDrivingOut",
            "SwitchOpenNeutralFIB5",
            "HeistLocate",
            "MP_job_load",
            "RaceTurbo",
            "MP_intro_logo",
            "HeistTripSkipFade",
            "MenuMGHeistOut",
            "MP_corona_switch",
            "MenuMGSelectionTint",
            "SuccessNeutral",
            "ExplosionJosh3",
            "SniperOverlay",
            "RampageOut",
            "Rampage",
            "Dont_tazeme_bro"
        };

        private static string ScreenEffectToString(ScreenEffect screenEffect)
        {
            if (screenEffect >= 0 && (int) screenEffect <= Eff.Length)
                return Eff[(int) screenEffect];
            return "INVALID";
        }

        public static void StartScreenEffect(ScreenEffect effectName, int duration = 0, bool looped = false)
        {
            Function.Call(Hash._START_SCREEN_EFFECT, ScreenEffectToString(effectName), duration, looped);
        }

        public static void StopAllScreenEffects()
        {
            Function.Call(Hash._STOP_ALL_SCREEN_EFFECTS);
        }

        public static void StopScreenEffect(ScreenEffect screenEffect)
        {
            Function.Call(Hash._STOP_SCREEN_EFFECT, ScreenEffectToString(screenEffect));
        }

        public static bool IsScreenEffectActive(ScreenEffect screenEffect)
        {
            return Function.Call<bool>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, ScreenEffectToString(screenEffect));
        }

        /// <summary>
        ///     Draw a marker in the world.
        /// </summary>
        /// <param name="type">The type of marker to draw.</param>
        /// <param name="position">The position of the marker.</param>
        /// <param name="direction">The direction of the marker will point. Alternatively you can use rotation.</param>
        /// <param name="rotation">The rotation of said marker.</param>
        /// <param name="scale">The scale on the x, y, and z axes of the marker.</param>
        /// <param name="color">The color of the marker; including alpha.</param>
        /// <param name="bobUpAndDown">If true this marker will bob up and down constantly.</param>
        /// <param name="faceCamera">If true this marker will always face the direction of the camera.</param>
        /// <param name="rotate">If true this marker will rotate around it's up vector.</param>
        /// <param name="drawOnEntities">This doesn't seem to work.</param>
        public static void DrawMarker(GTAMarkerType type, Vector3 position, Vector3 direction, Vector3 rotation,
            Vector3 scale, Color color, bool bobUpAndDown, bool faceCamera, bool rotate, bool drawOnEntities)
        {
            Function.Call(Hash.DRAW_MARKER, (int) type,
                position.X, position.Y, position.Z,
                direction.X, direction.Y, direction.Z,
                rotation.X, rotation.Y, rotation.Z,
                scale.X, scale.Y, scale.Z,
                color.R, color.G, color.B, color.A,
                bobUpAndDown, faceCamera,
                2, rotate, 0, 0, drawOnEntities);
        }

        /// <summary>
        ///     Draw's a line from 'start' to 'end' with the color 'color'.
        /// </summary>
        /// <param name="start">The start of the line.</param>
        /// <param name="end">The end of the line.</param>
        /// <param name="color">The color of the line, including alpha.</param>
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            Function.Call(Hash.DRAW_LINE, start.X, start.Y, start.Z, end.X, end.Y, end.Z,
                color.R, color.G, color.B, color.A);
        }

        /// <summary>
        ///     Start's a non-looped particle effect at the specified position with the specified rotation.
        /// </summary>
        /// <param name="assetName">The asset name / dictionary that defines the particle effect.</param>
        /// <param name="particleName">The name of the particle.</param>
        /// <param name="position">The position of the particle.</param>
        /// <param name="rotation">The rotation of the particle.</param>
        /// <param name="scale">The particle scale.</param>
        /// <returns></returns>
        public static Particle StartParticle(string assetName, string particleName,
            Vector3 position, Vector3 rotation, float scale, bool xRot = true, bool yRot = true, bool zRot = true)
        {
            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, assetName);
            return new Particle(Function.Call<int>(Hash._0x25129531F77B9ED3, particleName,
                position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z,
                scale, xRot, yRot, zRot));
        }

        /// <summary>
        ///     Get's the size of the game's screen resolution.
        /// </summary>
        /// <returns></returns>
        public static Size GetScreenResolution()
        {
            unsafe
            {
                int x;
                int y;
                Function.Call(Hash.GET_SCREEN_RESOLUTION, &x, &y);
                return new Size(x, y);
            }
        }

        /// <summary>
        ///     Creates a decal in the world.
        /// </summary>
        /// <param name="decalType">The type of decal.</param>
        /// <param name="position">The position of the decal.</param>
        /// <param name="normal">The decal normal.</param>
        /// <param name="width">The decal width.</param>
        /// <param name="height">The decal height.</param>
        /// <param name="color">The decal color.</param>
        /// <param name="timeout">How long the decal exists.</param>
        public static void AddDecal(Vector3 pos, DecalType decalType,
            float width = 1.0f, float height = 1.0f, float rCoef = 0.1f,
            float gCoef = 0.1f, float bCoef = 0.1f, float opacity = 1.0f, float timeout = 20.0f)
        {
            Function.Call<int>(Hash.ADD_DECAL, (int) decalType, pos.X, pos.Y, pos.Z, 0, 0, -1.0, 0, 1.0, 0, width,
                height, rCoef, gCoef, bCoef, opacity, timeout, 0, 0, 0);
        }
    }
}