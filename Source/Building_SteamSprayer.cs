using RimWorld;
using System;
using Verse;
using Verse.Sound;

namespace SteamCorp
{
    class Building_SteamSprayer : Building_Steam
    {
        private IntermittentSteamSprayer steamSprayer;

        private Sustainer spraySustainer;

        private int spraySustainerStartTick = -999;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            steamSprayer = new IntermittentSteamSprayer(this)
            {
                startSprayCallback = new Action(StartSpray),
                endSprayCallback = new Action(EndSpray)
            };
        }
         
        private void StartSpray()
        { 
            spraySustainer = SoundDefOf.GeyserSpray.TrySpawnSustainer(new TargetInfo(Position, Map, false));
            spraySustainerStartTick = Find.TickManager.TicksGame;
        }

        private void EndSpray()
        {
            if (spraySustainer != null)
            {
                spraySustainer.End();
                spraySustainer = null;
            }
        }

        public override void Tick()
        {
            base.Tick();
            steamSprayer.SteamSprayerTick();
            if (spraySustainer != null && Find.TickManager.TicksGame > spraySustainerStartTick + 1000)
            {
                Log.Message("Geyser spray sustainer still playing after 1000 ticks. Force-ending.");
                spraySustainer.End();
                spraySustainer = null;
            }
        }
    }
}
