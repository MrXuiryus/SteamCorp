using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI;
using Verse;

namespace SteamCorp
{
    public class WorkGiver_SteamDrill : WorkGiver_Scanner
    {

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> thingList = new List<Thing>();
            foreach (Thing thing in pawn.Map.spawnedThings)
            {
                if(thing.def.defName == "MrXuiryus_CoalDrill" || thing.def.defName == "MrXuiryus_BrassDrill")
                {
                    thingList.Add(thing);
                }
            }
            return thingList;
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

            if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction)
            {
                return false;
            }
            if (!pawn.CanReserveAndReach(thing, PathEndMode.OnCell, Danger.None, 1))
            {
                return false;
            }
            CompSteamDrill compDeepDrill = thing.TryGetComp<CompSteamDrill>();
            return compDeepDrill.CanDrillNow() && !thing.IsBurning();
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 cell)
        {
            Building building = null;
            foreach (Thing thing in cell.GetThingList(pawn.Map))
            {
                if(thing.TryGetComp<CompSteamDrill>() != null)
                {
                    building = (Building)thing;
                    break;
                }
            }
            return new Job(DefDatabase<JobDef>.GetNamed(SteamDrillDefOf.SteamDrillName), cell);
        }
    }
}