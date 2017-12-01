namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     The flags that will determine what the raycasts check for.
    /// </summary>
    public enum ShapeTestFlags
    {
        IntersectMap = 1,
        IntersectVehicles = 2,
        IntersectPeds = 4,
        IntersectPeds2 = 8,
        IntersectObjects = 16,
        IntersectWater = 32,
        Unknown1 = 64,
        Unknown2 = 128,
        IntersectVegitation = 256,
        Everything = 511
    }
}