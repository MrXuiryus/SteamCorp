using Verse;

namespace SteamCorp
{
    public class CompProperties_DamageDealer : CompProperties
    {
        public int damageAmount;
        public DamageDef damageDef;
        public bool affectInteractionCell;
        public bool affectAdjacentCells;
        public bool damagesSelf;
        public int ticksBetweenDamage;
        public bool affectOwnCell;

        public CompProperties_DamageDealer() {
			compClass = typeof(CompDamageDealer);
        }
    }
}