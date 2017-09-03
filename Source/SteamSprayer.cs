using RimWorld;
using System;
using Verse;

namespace SteamCorp
{
    public class SteamSprayer
    {
        private int MinTicksBetweenSprays;

        private int MaxTicksBetweenSprays;

        private int MinSprayDuration;

        private int MaxSprayDuration;

        private float PressureCutoff;

        private float SmokeAmount;

        private const float SprayThickness = 0.6f;

        private Thing parent;

        private int ticksUntilSpray = 500;

        private int sprayTicksLeft = 0;

        public Action startSprayCallback;

        public Action endSprayCallback;

        public SteamSprayer(Thing parent, int minTicks = 500, int maxTicks = 2000, 
            int minDuration = 200, int maxDuration = 500, float smokeAmount = .5f, float steamSprayCutoff = 1800f)
        {
            this.parent = parent;
            MinTicksBetweenSprays = minTicks;
            MaxTicksBetweenSprays = maxTicks;
            MinSprayDuration = minDuration;
            MaxSprayDuration = maxDuration;
            SmokeAmount = smokeAmount;
            PressureCutoff = steamSprayCutoff;
        }

        public void SteamSprayerTick() 
        {
            if (sprayTicksLeft > 0)
            {
                sprayTicksLeft--;
                CompSteam comp = parent.TryGetComp<CompSteam>();
                if (Rand.Value < 0.6f
                    && (comp != null && comp.SteamNet.CurrentStoredEnergy() >= PressureCutoff)
                    && (parent.TryGetComp<CompFlickable>() == null || FlickUtility.WantsToBeOn(parent)))
                {
                    MoteMaker.ThrowSmoke(parent.TrueCenter(), parent.Map, SmokeAmount);
                    MoteMaker.ThrowAirPuffUp(parent.TrueCenter(), parent.Map);
                }
                if (sprayTicksLeft <= 0)
                {
                    endSprayCallback?.Invoke();
                    ticksUntilSpray = Rand.RangeInclusive(MinTicksBetweenSprays, MaxTicksBetweenSprays);
                }
            }
            else
            {
                ticksUntilSpray--;
                if (ticksUntilSpray <= 0)
                {
                    startSprayCallback?.Invoke();
                    sprayTicksLeft = Rand.RangeInclusive(MinSprayDuration, MaxSprayDuration);
                }
            }
        }
    }
}
