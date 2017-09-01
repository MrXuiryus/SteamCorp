using Verse;

namespace SteamCorp
{
    public static class StaticManager
    {
        private static SteamNetManager net;
        private static SteamBreakdownManager breakdowns;

        public static SteamNetManager Net
        {
            get => net ?? (net = new SteamNetManager(Find.VisibleMap, new SteamNetGrid(Find.VisibleMap)));
            set => net = value;
        }

        public static SteamBreakdownManager Breakdowns
        {
            get => breakdowns ?? (breakdowns = new SteamBreakdownManager(Find.VisibleMap));
            set => breakdowns = value;
        }
    }
}
