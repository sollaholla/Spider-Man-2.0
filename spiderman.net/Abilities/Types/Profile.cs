using spiderman.net.Abilities.Attributes;

namespace spiderman.net.Abilities.Types
{
    /// <summary>
    /// Defines specific settings for spidey's profile.
    /// </summary>
    public class ProfileSettings : ManagedSettings
    {
        public ProfileSettings()
        {
            GetDefault();
        }

        [SerializableProperty("Agility")]
        public float RunSpeedMultiplier { get; set; }

        [SerializableProperty("Combat")]
        public float PunchForceMultiplier { get; set; }

        [SerializableProperty("Combat")]
        public float PunchDamageMultiplier { get; set; }

        [SerializableProperty("Webbing")]
        public float WebZipForceMultiplier { get; set; }

        [SerializableProperty("Misc")]
        public int MaxHealth { get; set; }

        [SerializableProperty("Misc")]
        public float HealthRechargeMultiplier { get; set; }

        [SerializableProperty("Misc")]
        public string Skin { get; set; }

        public override void GetDefault()
        {
            RunSpeedMultiplier = 1f;
            PunchForceMultiplier = 1f;
            PunchDamageMultiplier = 1f;
            WebZipForceMultiplier = 1f;
            MaxHealth = 3000;
            HealthRechargeMultiplier = 10f;
            Skin = "PLAYER_ONE";
        }
    }
}
