namespace SteamCorp
{
    class CompProperties_SteamAlternator : CompProperties_Steam
    {
        public float wattsOfSteamIn = 1500;

        public float wattsOfPowerOut = 1000;

        public CompProperties_SteamAlternator()
        {
            compClass = typeof(CompSteamAlternator);
        }
    }
}
