using GTA;

namespace SpiderMan.ProfileSystem.SpiderManScript
{
    public class SpiderManDefaultProfile : Profile
    {
        public SpiderManDefaultProfile(string path) : base(path)
        {
        }

        [SerializableProperty("Agility", "The greater the value, the faster spidey will run.")]
        public float RunSpeedMultiplier { get; set; }

        [SerializableProperty("Agility", "If true, spidey will do a flip to avoid " +
                                         "hitting a vehicle when performing a web pull.")]
        public bool FlipAfterWebPull { get; set; }

        [SerializableProperty("Combat", "The greater the value, the stronger the force of spidey's hits.")]
        public float PunchForceMultiplier { get; set; }

        [SerializableProperty("Combat", "The greater the value, the more damage spidey does to enemies.")]
        public float PunchDamageMultiplier { get; set; }

        [SerializableProperty("Webbing", "The greater the value, the faster spidey will fling towards" +
                                         "his target when web zipping.")]
        public float WebZipForceMultiplier { get; set; }

        [SerializableProperty("Misc", "The maximum health for this character.")]
        public int MaxHealth { get; set; }

        [SerializableProperty("Misc", "The speed at which spidey's health recharges.")]
        public float HealthRechargeMultiplier { get; set; }

        [SerializableProperty("Misc", "The model to use when this profile is active.")]
        public string Skin { get; set; }

        public override void SetDefault()
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