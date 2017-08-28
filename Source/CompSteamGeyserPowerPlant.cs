using System;
using RimWorld;
using Verse;

namespace SteamCorp
{
    class CompSteamGeyserPowerPlant : CompSteamPowerPlant
    {
        private IntermittentSteamSprayer steamSprayer;

        private Building_SteamGeyser geyser;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            steamSprayer = new IntermittentSteamSprayer(parent);
        }

        public override void CompTick()
        {
            base.CompTick();
            //try to set geyser
            if (geyser == null)
            {
                geyser = (Building_SteamGeyser)parent.Map.thingGrid.ThingAt(parent.Position, ThingDefOf.SteamGeyser);
            }
            //run as long as geyser is found
            if (geyser != null)
            {
                geyser.harvester = (Building)parent;
                steamSprayer.SteamSprayerTick();
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (geyser != null)
            {
                geyser.harvester = null;
            }
        }
    }
}