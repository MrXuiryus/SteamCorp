using System.Collections.Generic;
using Verse;

namespace SteamCorp
{
    public class SteamBreakdownManager : MapComponent
    {
        public const int CheckIntervalTicks = 1041;

        private List<CompSteamBreakdownable> comps = new List<CompSteamBreakdownable>();

        private HashSet<Thing> brokenDownThings = new HashSet<Thing>();

        public HashSet<Thing> BrokenDownThings { get => brokenDownThings; }

        public SteamBreakdownManager(Map map) : base(map)
        { }

        public void Register(CompSteamBreakdownable c)
        {
            comps.Add(c);
            if (c.BrokenDown)
            {
                brokenDownThings.Add(c.parent);
            }
        }

        public void Deregister(CompSteamBreakdownable c)
        {
            comps.Remove(c);
            brokenDownThings.Remove(c.parent);
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckIntervalTicks == 0)
            {
                for (int i = 0; i < comps.Count; i++)
                {
                    comps[i].CheckForBreakdown();
                }
            }
        }

        public void Notify_BrokenDown(Thing thing)
        {
            brokenDownThings.Add(thing);
        }

        public void Notify_Repaired(Thing thing)
        {
            brokenDownThings.Remove(thing);
        }
    }
}
