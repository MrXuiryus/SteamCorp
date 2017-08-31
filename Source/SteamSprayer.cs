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

        private int SmokeAmount;

        private const float SprayThickness = 0.6f;

        private Thing parent;

        private int ticksUntilSpray = 500;

        private int sprayTicksLeft;

        public Action startSprayCallback;

        public Action endSprayCallback;

        public SteamSprayer(Thing parent, int minTicks = 500, int maxTicks = 2000, 
            int minDuration = 200, int maxDuration = 500, float smokeAmount = 2)
        {
            this.parent = parent;
            MinTicksBetweenSprays = minTicks;
            MaxTicksBetweenSprays = maxTicks;
            MinSprayDuration = minDuration;
            MaxSprayDuration = maxDuration;
        }

        public void SteamSprayerTick()
        {
            if (sprayTicksLeft > 0)
            {
                sprayTicksLeft--;
                if (Rand.Value < 0.6f)
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
