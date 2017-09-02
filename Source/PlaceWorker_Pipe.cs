using System.Collections.Generic;
using Verse;

namespace SteamCorp
{
    public class PlaceWorker_Pipe : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, 
            IntVec3 loc, 
            Rot4 rot, 
            Thing thingToIgnore = null)
        {
            
            List<Thing> thingList = loc.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].TryGetComp<CompSteam>() != null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

