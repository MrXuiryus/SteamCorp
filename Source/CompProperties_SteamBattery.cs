namespace SteamCorp
{
    public class CompProperties_SteamBattery : CompProperties_SteamSprayer
    {
        public float storedEnergyMax = 2000f;

        public float efficiency = 0.5f;

        public CompProperties_SteamBattery()
        {
            compClass = typeof(CompSteamBattery);
        }
    }
}
