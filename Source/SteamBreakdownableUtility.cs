using Verse;

namespace SteamCorp
{
    public static class SteamBreakdownableUtility
    {
        public static bool IsSteamBrokenDown(this Thing t)
        {
            CompSteamBreakdownable compBreakdownable = t.TryGetComp<CompSteamBreakdownable>();
            return compBreakdownable != null && compBreakdownable.BrokenDown;
        }
    }
}
