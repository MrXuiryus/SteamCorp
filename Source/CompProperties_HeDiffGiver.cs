using Verse;

namespace HediffDamageComps
{
    public class CompProperties_HediffGiver : CompProperties
    {
        public int damageAmount;
        public HediffDef hediffDef;
        public DamageDef damageDef;
        public bool affectInteractionCell;
        public bool affectAdjacentCells;
        public bool affectsSelf;
        public int ticksBetweenEffect;

        public CompProperties_HediffGiver() {
			compClass = typeof(CompHediffGiver);
        }
    }
}