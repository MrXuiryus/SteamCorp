using Verse;

namespace SteamCorp
{
    public class CompProperties_SteamBreakdownable : CompProperties_Steam
    {
        public CompProperties_SteamBreakdownable()
        {
            compClass = typeof(CompSteamBreakdownable);
        }
    }
}
