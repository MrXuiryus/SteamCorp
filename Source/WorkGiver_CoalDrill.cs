using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI;
using Verse;

namespace SteamCorp
{
    public class WorkGiver_CoalDrill : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> ret = new List<Thing>();
            ret.AddRange(pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("MrXuiryus_CoalDrill")).Cast<Thing>());
            ret.AddRange(pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("MrXuiryus_AdvancedCoalDrill")).Cast<Thing>());
            return ret;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                if (allBuildingsColonist[i].def == ThingDef.Named("MrXuiryus_CoalDrill")
                    || allBuildingsColonist[i].def == ThingDef.Named("MrXuiryus_AdvancedCoalDrill"))
                {
                    CompSteamTrader comp = allBuildingsColonist[i].GetComp<CompSteamTrader>();
                    if (comp == null || comp.SteamOn)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (pawn == null)
            {
                return false;
            }

            if (thing.IsForbidden(pawn))
            {
                return false;
            }

            if (thing is Blueprint || thing is Frame)
            {
                return false;
            }
            Building building = thing as Building;
            if (building == null)
            {
                return false;
            }
            if (!pawn.CanReserve(thing, 1, -1, null, forced))
            {
                return false;
            }
            CompSteamDrill compDeepDrill = thing.TryGetComp<CompSteamDrill>();
            CompSteamTrader compTrader = thing.TryGetComp<CompSteamTrader>();
            return base.HasJobOnThing(pawn, thing, forced)
                && compDeepDrill.CanDrillNow() && !thing.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(SteamDrillDefOf.SteamDrillName), thing, 1500, forced);
        }
    }
}