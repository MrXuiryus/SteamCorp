using RimWorld;
using UnityEngine;
using Verse;

namespace SteamCorp
{
    class CompSteamDrill : ThingComp
    {
        private CompPowerTrader powerComp;

        private CompSteamTrader steamComp;

        private float lumpProgress;

        public bool JustProducedLump
        {
            get => lumpProgress == 0;
        }

        public CompProperties_SteamDrill Props
        {
            get => (CompProperties_SteamDrill)props;
        }

        public float PercentDone
        {
            get => lumpProgress / Props.workNeeded;
        }

        private float lumpYieldPct;

        public float ProgressToNextLumpPercent
        {
            get
            {
                return lumpProgress / Props.workNeeded;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            powerComp = parent.TryGetComp<CompPowerTrader>();
            steamComp = parent.TryGetComp<CompSteamTrader>();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref lumpProgress, "lumpProgress", 0f, false);
            Scribe_Values.Look(ref lumpYieldPct, "lumpYieldPct", 0f, false);
        }

        public void DrillWorkDone(Pawn driller)
        {
            float statValue = driller.GetStatValue(StatDefOf.MiningSpeed, true);
            lumpProgress += statValue;
            lumpYieldPct += statValue * driller.GetStatValue(StatDefOf.MiningYield, true) / Props.workNeeded;
            if (lumpProgress > Props.workNeeded)
            {
                TryProduceLump(lumpYieldPct);
                lumpProgress = 0f;
                lumpYieldPct = 0f;
            }
        }

        private void TryProduceLump(float yieldPct)
        {
            if (TryGetNextResource(out ThingDef thingDef, out int num, out IntVec3 c))
            {
                int stackCount = Mathf.Max(1, GenMath.RoundRandom(num * yieldPct));
                Thing thing = ThingMaker.MakeThing(thingDef, null);
                thing.stackCount = stackCount;
                GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null);
            }
            else
            {
                Log.Error("Drill tried to ProduceLump but couldn't.");
            }
        }

        public bool TryGetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            resDef = (parent.def.defName == "MrXuiryus_CoalDrill" || parent.def.defName == "MrXuiryus_AdvancedCoalDrill") 
                ? ThingDef.Named("MrXuiryus_Coal") : ThingDef.Named("MrXuiryus_Brass");
            countPresent = Props.stackAmount;
            cell = parent.Position;
            return true;
        }

        public bool CanDrillNow()
        {
            return (powerComp == null || powerComp.PowerOn) && (steamComp == null || steamComp.SteamOn) && ResourcesPresent();
        }

        public bool ResourcesPresent()
        {
            return true;
        }

        public override string CompInspectStringExtra()
        {
            ThingDef thingDef;
            int num;
            IntVec3 intVec;
            if (TryGetNextResource(out thingDef, out num, out intVec))
            {
                return string.Concat(new string[]
                {
                    "ResourceBelow".Translate(),
                    ": ",
                    thingDef.label,
                    "\n",
                    "ProgressToNextLump".Translate(),
                    ": ",
                    ProgressToNextLumpPercent.ToStringPercent("F0")
                });
            }
            return "ResourceBelow".Translate() + ": " + "NothingLower".Translate();
        }
    }
}
