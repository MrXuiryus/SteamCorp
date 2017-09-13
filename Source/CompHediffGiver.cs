using System.Collections.Generic;
using Verse;
using RimWorld;

namespace SteamCorp
{
    public class CompHediffGiver : ThingComp
    {
        private float ticks = 0;
        public CompProperties_HediffGiver Props {
            get => (CompProperties_HediffGiver) props;
        }
        
        public override void CompTick()
        {
            base.CompTick();
            ticks += 1;
            if (ticks % Props.ticksBetweenEffect == 0)
            {
                HashSet<Thing> affectedThings = new HashSet<Thing>();
                if (!parent.Spawned)
                {
                    return;
                }

                if (parent.TryGetComp<CompPowerTrader>() != null
                    && !parent.TryGetComp<CompPowerTrader>().PowerOn)
                {
                    return;
                }

                if (parent.TryGetComp<CompSteamTrader>() != null
                    && !parent.TryGetComp<CompSteamTrader>().SteamOn)
                {
                    return;
                }

                //populate potential victim list
                if (Props.affectAdjacentCells)
                {
                    foreach (IntVec3 cell in parent.CellsAdjacent8WayAndInside())
                    {
                        if (Props.affectOwnCell || cell != parent.Position)
                        {
                            affectedThings.AddRange(cell.GetThingList(parent.Map));
                        }
                    }
                }
                else
                {
                    if (Props.affectInteractionCell)
                    {
                        affectedThings.AddRange(parent.InteractionCell.GetThingList(parent.Map));
                    }
                    if (Props.affectOwnCell)
                    {
                        affectedThings.AddRange(parent.Position.GetThingList(parent.Map));
                    }
                }

                //remove item from list if it doesn't damage itself
                if (!Props.affectsSelf)
                {
                    affectedThings.Remove(parent);
                }

                foreach(Thing t in affectedThings)
                {
                    if (t is Pawn)
                    {
                        ((Pawn)t).health.AddHediff(Props.hediffDef, null, null);
                    }
                }

                ticks = 0;
            }
        }
    }
}
