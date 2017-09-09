using RimWorld;
using System;
using Verse;
using Verse.Sound;

namespace SteamCorp
{
    public class CompSteamSprayer : CompSteam
    { 
        private SteamSprayer steamSprayer;
         
        private Sustainer spraySustainer;

        private int spraySustainerStartTick = -999;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props != null)  
            {
                steamSprayer = new SteamSprayer(parent, Props.MinTicksBetweenSprays, Props.MaxTicksBetweenSprays,
                    Props.MinSprayDuration, Props.MaxSprayDuration, Props.SmokeAmount, Props.PressureCutoff)
                {
                    startSprayCallback = new Action(StartSpray),
                    endSprayCallback = new Action(EndSpray)
                }; 
            } 
            else 
            {
                steamSprayer = new SteamSprayer(parent)
                {
                    startSprayCallback = new Action(StartSpray),
                    endSprayCallback = new Action(EndSpray)
                };
            }
        }

        private void StartSpray()
        {
            spraySustainer = SoundDefOf.GeyserSpray.TrySpawnSustainer(
                new TargetInfo(parent.Position, parent.Map, false));
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

        public override void CompTick()
        {
            base.CompTick();
            steamSprayer.SteamSprayerTick();
            if (spraySustainer != null && Find.TickManager.TicksGame > spraySustainerStartTick + 1000)
            {
                Log.Message("Geyser spray sustainer still playing after 1000 ticks. Force-ending.");
                spraySustainer.End();
                spraySustainer = null;
            }
        }

        public new CompProperties_SteamSprayer Props
        {
            get => (CompProperties_SteamSprayer)props;
        }
    }
}