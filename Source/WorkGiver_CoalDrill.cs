using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using Verse;

namespace SteamCorp
{
    public class WorkGiver_CoalDrill : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest 
        {
            get
            {
                return ThingRequest.ForDef(ThingDef.Named("MrXuiryus_CoalDrill"));
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> thingList = new List<Thing>();
            foreach (Thing thing in pawn.Map.spawnedThings)
            {
                if(thing.def.defName == "MrXuiryus_CoalDrill"
                    && pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.None)
                    && thing.TryGetComp<CompSteamTrader>()!= null 
                    && thing.TryGetComp<CompSteamTrader>().SteamOn)
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
             
            if (thing is Blueprint || thing is Frame)
            {
                return false;
            }
            if (!pawn.CanReserve(thing))
            {
                return false;
            }
            CompSteamDrill compDeepDrill = thing.TryGetComp<CompSteamDrill>();
            CompSteamTrader compTrader = thing.TryGetComp<CompSteamTrader>();
            return base.HasJobOnThing(pawn, thing, forced) 
                && compDeepDrill.CanDrillNow() && !thing.IsBurning() && compTrader.SteamOn 
                && pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.None);
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(SteamDrillDefOf.SteamDrillName), thing, 1500, forced);
        }
    }
}