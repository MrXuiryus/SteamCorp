using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SteamCorp
{
    class JobDriver_SteamDrill : JobDriver
    {
        private float workLeft = -1000f;

        private float baseWorkAmount = 100;

        protected float BaseWorkAmount
        {
            get
            {
                return baseWorkAmount;
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
            Toil reservation = Toils_Reserve.Reserve(TargetIndex.A, 1, -1);
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
                    float miningSpeed = (SpeedStat == null) ? 1f : pawn.GetStatValue(SpeedStat, true);
                    if (actor.skills != null)
                    {
                        actor.skills.Learn(SkillDefOf.Mining, 0.11f, false);
                    }
                    Thing thing = (Thing)reservation.actor.CurJob.targetA;
                    CompSteamDrill comp = thing.TryGetComp<CompSteamDrill>();
                    comp.DrillWorkDone(actor);
                    if (comp.JustProducedLump)
                    {
                        ReadyForNextToil();
                    }
                    return;
                }
            };
            doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            doWork.WithProgressBar(TargetIndex.A, () => ((Thing)reservation.actor.CurJob.targetA).TryGetComp<CompSteamDrill>().PercentDone, false, -0.5f);
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            yield return doWork;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 0f, false);
        }
    }
}
