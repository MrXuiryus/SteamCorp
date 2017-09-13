namespace SteamCorp
{
    public class CompProperties_SteamDrill : CompProperties_Steam
    {
        public float storedEnergyMax = 2000f;

        public float efficiency = 0.5f;

        public int stackAmount = 30;

        public float workNeeded = 6000f;

        public CompProperties_SteamDrill()
        {
            compClass = typeof(CompSteamBattery);
        }
    }
}
