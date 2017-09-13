using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HediffDamageComps
{
    public class CompDamageDealer : ThingComp
    {
        private float ticks = 0;
        public CompProperties_DamageDealer Props {
            get => (CompProperties_DamageDealer) props;
        }

        public override void CompTick()
        {
            ticks += 1;
            base.CompTick();
            if (ticks % Props.ticksBetweenDamage == 0)
            {
                HashSet<Thing> affectedThings = new HashSet<Thing>();
                if (!parent.Spawned)
                {
                    return;
                }

                if(parent.TryGetComp<CompPowerTrader>() != null 
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
                if (!Props.damagesSelf)
                {
                    affectedThings.Remove(parent);
                }

                foreach(Thing t in affectedThings)
                {
                    t.TakeDamage(new DamageInfo(Props.damageDef, Props.damageAmount, -1, parent));
                }

                ticks = 0;
            }

        }
    }
}
