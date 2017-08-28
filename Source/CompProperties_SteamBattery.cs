using System;
using RimWorld;

namespace SteamCorp
{
    public class CompProperties_SteamBattery : CompProperties_Steam
    {
        public float storedEnergyMax = 1000f;

        public float efficiency = 0.5f;

        public CompProperties_SteamBattery()
        {
            compClass = typeof(CompSteamBattery);
        }
    }
}
