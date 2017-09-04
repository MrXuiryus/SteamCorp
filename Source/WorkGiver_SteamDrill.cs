using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI;
using Verse;

namespace SteamCorp
{
    public class WorkGiver_SteamDrill : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForDef(ThingDef.Named("MrXuiryus_BrassDrill"));
            }
        }

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
            if (!pawn.CanReserve(thing))
            {
                return false;
            }
            CompSteamDrill compDeepDrill = thing.TryGetComp<CompSteamDrill>();
            return compDeepDrill.CanDrillNow() && !thing.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(SteamDrillDefOf.SteamDrillName), thing, 1500, forced);
        }
    }
}