using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SteamCorp
{
    class JobDriver_SteamDrill : JobDriver
    {
        private float workLeft = -1000f;

        protected float BaseWorkAmount
        {
            get
            {
                return 200;
            }
        }

        protected StatDef SpeedStat
        {
            get
            {
                return StatDefOf.MiningSpeed;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("MakeNewToils");
            ReservationLayerDef floor = ReservationLayerDefOf.Floor;
            Toil reservation = Toils_Reserve.Reserve(TargetIndex.A, 1, -1, floor);
            yield return reservation;
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
            Toil doWork = new Toil
            {
                initAction = delegate
                {
                    workLeft = BaseWorkAmount;
                },
                tickAction = delegate
                {
                    Pawn actor = CurToil.actor;
                    float num = (SpeedStat == null) ? 1f : pawn.GetStatValue(SpeedStat, true);
                    workLeft -= num;
                    if (actor.skills != null)
                    {
                        actor.skills.Learn(SkillDefOf.Mining, 0.11f, false);
                    }
                    if (workLeft <= 0f)
                    {
                        Thing thing = reservation.actor.TargetCurrentlyAimingAt.Thing;
                        Thing toPlace = null;
                        if (thing.def.defName == "MrXuiryus_CoalDrill")
                        {

                            toPlace = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MrXuiryus_Coal", true), null);
                        }
                        else
                        {

                            toPlace = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MrXuiryus_Brass", true), null);
                        }
                        thing.stackCount = 75;
                        GenPlace.TryPlaceThing(thing, TargetLocA, Map, ThingPlaceMode.Near, null);
                        ReadyForNextToil();
                        return;
                    }

                }
            };
            doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            doWork.WithProgressBar(TargetIndex.A, () => 1f - workLeft / BaseWorkAmount, false, -0.5f);
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            yield return doWork;

        }

        public override void ExposeData()
        {
            Log.Message("ExposeData");
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 0f, false);
        }
    }
}
