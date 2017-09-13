using System.Collections.Generic;
using Verse;
using RimWorld;

namespace HediffDamageComps
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

                //populate potential victim list
                if (Props.affectAdjacentCells)
                {
                    foreach (IntVec3 cell in parent.CellsAdjacent8WayAndInside())
                    {
                        affectedThings.AddRange(cell.GetThingList(parent.Map));
                    }
                }
                else if (Props.affectInteractionCell)
                {
                    affectedThings.AddRange(parent.InteractionCell.GetThingList(parent.Map));
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
